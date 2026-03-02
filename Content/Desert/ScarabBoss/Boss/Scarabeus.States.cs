using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	#region Cinematics
	public void SpawnAnimation()
	{
		const int swarm_time = 120;
		const int roar_time = 120;

		/*Todo: 
		 * foreground scarab particles fly across the screen from bottom left to top right
		 * screenshake
		 * ground beneath player starts emitting particles
		 * scarab bursts out of ground and roars
		*/

		if (Counter > swarm_time + roar_time) //End the cinematic
		{
			NPC.dontTakeDamage = false;
			ChangeState(Walking);
		}
		else if (Counter > swarm_time) //Emerge
		{
			if (NPC.Opacity == 0) //One-time effects
			{
				NPC.noTileCollide = false;
				NPC.noGravity = false;
				NPC.velocity.Y = -12;
				NPC.Opacity = 1;
			}

			if (Grounded) //Landed
			{
				NPC.FaceTarget();
				UpdateFrame(6, 12, PhaseOneProfile, false);
				NPC.rotation = 0;

				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 2, 3, 20));
			}
			else
			{
				SetFrame(DigFrame, PhaseOneProfile);
				NPC.rotation += 0.3f * NPC.direction;
			}
		}
		else //Rumbling
		{
			if (Counter == 0) //On-spawn effects
			{
				NPC.Center = FindGroundFromPosition(Target.Center) - new Vector2(0, NPC.height / 2);
				NPC.FaceTarget();

				if (!Main.dedServ)
				{
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitX, 0.5f, 3, swarm_time * 2));

					for (int i = 0; i < 48; i++)
					{
						Vector2 scarabPos = Target.Center;
						bool backgroundScarab = !Main.rand.NextBool(3);
						int spawnDelayRange = (int)(swarm_time * (backgroundScarab ? 0.25f : 0.66f));
						int spawnDelayStatic = backgroundScarab ? 0 : swarm_time / 3;
						scarabPos += new Vector2(-Main.rand.NextFloat(900, 1400), Main.rand.NextFloat(200, 800)) * (backgroundScarab ? 1f : 1.2f);
						ParticleHandler.SpawnQueuedParticle(new ScarabParticle(scarabPos, Main.rand.NextFloat(0.3f, 0.7f), 1, backgroundScarab), Main.rand.Next(spawnDelayRange) + spawnDelayStatic);
					}
				}
			}

			if (!Main.dedServ)
			{
				Rectangle area = new((int)NPC.BottomLeft.X , (int)NPC.BottomLeft.Y, NPC.width, 2);
				for (int i = 0; i < Main.rand.Next(4); i++)
				{
					Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);

					ParticleHandler.SpawnParticle(new SmokeCloud(Main.rand.NextVector2FromRectangle(area), particleVel, new Color(223, 219, 147) * 2f, Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
					{
						Pixellate = true,
						DissolveAmount = 1,
						Intensity = 0.9f,
						SecondaryColor = new Color(188, 170, 86) * 1.33f,
						TertiaryColor = new Color(58, 49, 18) * 0.5f,
						PixelDivisor = 3,
						Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
						ColorLerpExponent = 0.5f,
						Layer = ParticleLayer.BelowSolid
					});
				}

				if (Main.rand.NextBool(3))
					Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(area), DustID.Sand, new(0, -4), 0, default, Main.rand.NextFloat(0.7f, 1.2f));

				if (Counter % 20 == 0)
					BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX + NPC.velocity / 2);
			}
		}
	}
	#endregion

	#region Phase 1
	public void Walking()
	{
		const int max_walk_time = 360;

		ref float digTimer = ref NPC.ai[2];
		ref float jumpTimer = ref NPC.ai[3];

		NPC.knockBackResist = 0.7f;
		NPC.FaceTarget();

		if (NPC.velocity == Vector2.Zero && ++digTimer > 30)
		{
			ChangeState(Dig);
			return;
		}

		if (Grounded) //Check if grounded
		{
			if (DetermineGap() && ++jumpTimer > 15) // Jump over gaps if needed
			{
				ChangeState(Leap);
				return;
			}

			float distance = NPC.DistanceSQ(Target.Center);
			if (distance > 200 * 200)
			{
				NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + NPC.direction * 0.3f, -5, 5);
			}
			else
			{
				NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X - NPC.direction * 0.1f, -5, 5);

				if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(50))
					ChangeState(HornSwipe);
			}

			NPC.Step();
		}

		float fps = Math.Min(NPC.velocity.X * 5, 12) * NPC.direction;
		UpdateFrame(1, (int)fps, PhaseOneProfile);

		if (Counter > max_walk_time)
			ChangeState(SelectWeightedState());
	}

	/// <summary> Determines if this NPC, while moving, is approaching a gap that requires jumping over. </summary>
	private bool DetermineGap()
	{
		if (NPC.velocity.X == 0)
			return false;
		else if (NPC.velocity.X < 0)
			return !Collision.SolidCollision(NPC.BottomLeft - new Vector2(NPC.width * 0.6f, 0), (int)(NPC.width * 0.6f), 16);

		return !Collision.SolidCollision(NPC.BottomRight, (int)(NPC.width * 0.6f), 16);
	}

	public void HornSwipe()
	{
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;

		NPC.velocity.X *= 0.5f;
		FrameState state = UpdateFrame(2, 12, PhaseOneProfile, false);

		if (currentFrame.Y is > 2 and < 7)
			dealContactDamage = true;

		if (state == FrameState.Stopped)
			ChangeState(SelectWeightedState());
	}

	public void Skitter()
	{
		const int skitter_time = 40;

		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		NPC.velocity.X = -NPC.direction * MathHelper.Lerp(12, 4, EaseFunction.EaseQuadOut.Ease(Counter / skitter_time));
		NPC.Step();

		UpdateFrame(1, (int)(NPC.direction * NPC.velocity.X) * 4, PhaseOneProfile);

		if (Counter > skitter_time)
			ChangeState(SelectWeightedState());
	}

	public void Leap()
	{
		ref float jumpState = ref NPC.ai[2];

		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		NPC.GravityMultiplier *= 2;

		switch (jumpState)
		{
			case 0: //Prepare for a jump
				if (Grounded) //Check if grounded
				{
					NPC.velocity.X *= 0.8f;
					NPC.FaceTarget();

					if (UpdateFrame(4, 10, PhaseOneProfile, false) == FrameState.Stopped) //Add jump velocity
					{
						Vector2 desiredPos = Target.Center + Target.velocity + NPC.direction * 112 * Vector2.UnitX;
						NPC.velocity = NPC.GetArcVel(desiredPos, NPC.gravity, 15, true);
						NPC.noTileCollide = true;

						Counter = 0;
						jumpState++;
					}
				}

				break;

			case 1: //Jump and land
				SetFrame(0, 2, PhaseOneProfile);
				dealContactDamage = true;

				NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
				NPC.noTileCollide = NPC.velocity.Y < 0;

				if (Grounded) //Land
				{
					NPC.rotation = 0;
					jumpState++;

					if (!Main.dedServ)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 4, 2, 15, -1, "ScarabLanding"));
						Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
					}
				}

				break;

			case 2: //Recover
				NPC.noTileCollide = false;
				NPC.velocity.X *= 0.9f;

				if (UpdateFrame(6, 12, PhaseOneProfile, false) == FrameState.Stopped)
				{
					SetFrame(0, 0, PhaseOneProfile); //Return to the control frame
					ChangeState(SelectWeightedState());
				}

				break;
		}

		/*
		 * Todo:
		 * Phase through some tiles but avoid phasing through a wall, use primarily for closing vertical gaps, pits, and walls
		 */
	}

	public void TraversalLeap()
	{
		ref float jumpState = ref NPC.ai[2];

		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		NPC.GravityMultiplier *= 2;

		switch (jumpState)
		{
			case 0: //Prepare for a jump
				if (Grounded) //Check if grounded
				{
					NPC.velocity.X *= 0.8f;
					NPC.FaceTarget();

					if (UpdateFrame(4, 10, PhaseOneProfile, false) == FrameState.Stopped) //Add jump velocity
					{
						Vector2 desiredPos = Target.Center;
						NPC.velocity = NPC.GetArcVel(desiredPos, NPC.gravity, Math.Max(15, (NPC.Center.Y - desiredPos.Y) / 18f), true);
						NPC.noTileCollide = true;

						jumpState++;
					}
				}

				break;

			case 1: //Jump and land
				SetFrame(0, 2, PhaseOneProfile);
				dealContactDamage = true;

				NPC.noTileCollide = NPC.velocity.Y < 0;

				if (Grounded) //Land
				{
					Counter = 0;
					jumpState++;
					//vfx and sfx and shockwaves here

					if (!Main.dedServ)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 4, 2, 15, -1, "ScarabLanding"));
						Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
					}
				}

				break;

			case 2: //Recover
				NPC.noTileCollide = false;
				NPC.velocity.X *= 0.9f;

				if (UpdateFrame(6, 12, PhaseOneProfile, false) == FrameState.Stopped)
				{
					SetFrame(0, 0, PhaseOneProfile); //Return to the control frame
					ChangeState(SelectWeightedState());
				}

				break;
		}
	}

	public void RollDash()
	{
		const int dash_time = 20;
		const int transition_time = 40;

		ref float dashState = ref NPC.ai[2];

		NPC.noTileCollide = false;
		NPC.noGravity = false;

		if (dashState == 0) //Prepare for a roll
		{
			NPC.velocity.X *= 0.8f;
			NPC.FaceTarget();

			if (UpdateFrame(3, 12, PhaseOneProfile, false) == FrameState.Stopped)
			{
				dashState++;
				Counter = 0;
			}

			return;
		}

		if (Counter > dash_time + transition_time) //End
		{
			NPC.velocity.X /= 2;
			ChangeState(Walking);
		}
		else if (Counter > dash_time) //Skid to a stop
		{
			SetFrame(0, 6, PhaseOneProfile);
			NPC.rotation = 0;
			NPC.velocity.X *= 0.94f;

			if (Math.Sign(NPC.velocity.X) is int newDirection && newDirection != 0)
				NPC.direction = -newDirection;

			if (Math.Abs(NPC.velocity.X) > 1 && !Main.dedServ && Main.rand.NextBool())
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -Vector2.UnitY, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.05f, 0.25f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.25f
				});

				Dust.NewDust(NPC.BottomLeft, NPC.width, 16, DustID.Sand, 0, Main.rand.NextFloat(-4, -8), 0, default, Main.rand.NextFloat(0.5f, 0.9f));
			}
		}
		else //Rolling
		{
			NPC.velocity.X = NPC.direction * 28;
			NPC.rotation += 0.3f * NPC.spriteDirection;
			NPC.Step();

			SetFrame(DigFrame, PhaseOneProfile);
			showTrail = true;
			dealContactDamage = true;
			//sfx here

			if (NPC.collideX)
				ChangeState(BounceGroundPound); //bounce off of surfaces
		}
	}

	public void GroundedSlam()
	{
		const int duration = 90;

		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		NPC.velocity.X *= 0.8f;

		if (Counter < 5)
			NPC.FaceTarget();

		UpdateFrame(7, (int)(Profile.GetFrameCount(7) * 60f / duration), PhaseOneProfile);

		if (Counter == (int)(duration * 0.6f))
		{
			dealContactDamage = true;
			//projectiles and sfx here

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 center = FindGroundFromPosition(NPC.Center + NPC.direction * Vector2.UnitX * 320);
				Projectile.NewProjectile(NPC.GetSource_FromThis(), center - Vector2.UnitY * 160, Vector2.Zero, ModContent.ProjectileType<SlamShockwave>(), NPC.damage / 2, 16, Main.myPlayer, NPC.direction);
			}
		}

		if (Counter > duration)
			ChangeState(SelectWeightedState());
	}

	public void BounceGroundPound()
	{
		const int max_bounces = 3;
		const int final_bounce_track_time = 40;
		const int air_pause_time = 20;
		const int rest_time = 90;

		ref float jumpState = ref NPC.ai[2];

		bool isGravityAllowed = true;

		NPC.noTileCollide = false;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;

		if (jumpState < max_bounces)
		{
			SetFrame(DigFrame, PhaseOneProfile);
			dealContactDamage = true;

			//Check if grounded
			if (Grounded)
			{
				jumpState++;
				NPC.velocity.Y = -16;
				NPC.FaceTarget();

				if (!Main.dedServ)
				{
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 2, 3, 15, 800));
					Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
				}
			}
			else
			{
				BounceTracking();
			}
		}
		else if (jumpState == max_bounces)
		{
			dealContactDamage = true;

			//Continue tracking in the air for a bit
			if (Counter < final_bounce_track_time)
			{
				BounceTracking();

				if (Counter > final_bounce_track_time - 10)
					NPC.velocity.X *= 0.9f;
			}
			else
			{
				NPC.rotation += NPC.direction * 0.3f;

				if (Counter < final_bounce_track_time + air_pause_time)
				{
					isGravityAllowed = false;
					NPC.velocity.X = 0;
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -1.25f, 0.3f);
				}

				else if (Counter == final_bounce_track_time + air_pause_time)
					NPC.velocity.Y = 16;
			} //Pause and spin faster in air and slam down

			//On tile collision
			if (Counter > final_bounce_track_time + air_pause_time && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				jumpState++; //use the variable to track the final ground pound too
				NPC.velocity.Y = -4;

				for (int i = -3; i <= 3; i++)
				{
					if (i == 0)
						continue;

					float distStep = Main.rand.NextFloat(11, 13) * i * 16;
					Vector2 projPosition = FindGroundFromPosition(NPC.Bottom + Vector2.UnitX * distStep) - Vector2.UnitY * 80;

					Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3, Main.myPlayer, Math.Abs(i) * 40);
				}

				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 2, 3, 15));
			}
		}
		else //rest before next attack
		{
			UpdateFrame(4, (int)(8 * rest_time / 60f), PhaseOneProfile, false);

			if (currentFrame.Y < 7)
				currentFrame.Y = 7;

			NPC.velocity *= 0.5f;
			NPC.rotation = 0;
			NPC.noGravity = false;

			isGravityAllowed = false;

			if (Counter > final_bounce_track_time + air_pause_time + rest_time)
				ChangeState(SelectWeightedState());
		}

		if (isGravityAllowed)
			NPC.velocity.Y += 0.38f;

		void BounceTracking()
		{
			float desiredVel = (NPC.Center.X < Target.Center.X) ? 16 : -16;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.006f);

			if (NPC.velocity.Y < 12)
				NPC.velocity.Y += 0.08f;

			NPC.rotation += NPC.direction * 0.1f + NPC.velocity.X / 120;
		}
	}

	public void Dig()
	{
		const int dig_time = 120;

		ref float digState = ref NPC.ai[2];
		NPC.behindTiles = digState > 0;

		switch (digState)
		{
			case 0: //Prepare and leap
				if (UpdateFrame(3, 12, PhaseOneProfile, false) == FrameState.Stopped)
				{
					NPC.velocity.Y -= 4;
					NPC.velocity.X = NPC.direction * 4;

					digState++;
				}
				else
				{
					NPC.velocity.X *= 0.95f;
				}

				break;

			case 1: //Fall into the ground
				SetFrame(0, 1, PhaseOneProfile);

				NPC.rotation = NPC.velocity.Y * 0.04f * NPC.direction;
				NPC.velocity.Y += 0.5f;
				NPC.noTileCollide = true;

				if (Collision.SolidCollision(NPC.Top - new Vector2(4), 8, 8))
				{
					digState++; //Disappear into the ground
					Counter = 0;
				}

				break;

			case 2: //Dig
				NPC.Opacity = 0;
				NPC.noGravity = true;

				Vector2 groundPosition = FindGroundFromPosition(new Vector2(NPC.Center.X, Target.Center.Y));
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.DirectionTo(Target.Center).X * 8, 0.1f);
				NPC.position.Y = groundPosition.Y;

				if (!Main.dedServ) //Digging visuals
				{
					for (int i = 0; i < Main.rand.Next(4); i++)
					{
						Vector2 particlePos = NPC.Center - Vector2.UnitY * 48;
						particlePos += Main.rand.NextFloat(-64, 64) * Vector2.UnitX;

						Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);

						ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, new Color(223, 219, 147) * 2f, Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
						{
							Pixellate = true,
							DissolveAmount = 1,
							Intensity = 0.9f,
							SecondaryColor = new Color(188, 170, 86) * 1.33f,
							TertiaryColor = new Color(58, 49, 18) * 0.5f,
							PixelDivisor = 3,
							Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
							ColorLerpExponent = 0.5f,
							Layer = ParticleLayer.BelowSolid
						});
					}

					if (Main.rand.NextBool(3))
						Dust.NewDust(NPC.position - Vector2.UnitY * 8, NPC.width, 0, DustID.Sand, 0, -4, 0, default, Main.rand.NextFloat(0.7f, 1.2f));

					if (Counter % 20 == 0)
						BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX);
				}

				if (Counter > dig_time) //Reemerge
				{
					Counter = 0;
					digState++;

					NPC.velocity.Y = -10;
					NPC.noGravity = false;
					NPC.FaceTarget();
				}

				break;

			case 3: //Emerge and land
				SetFrame(DigFrame, PhaseOneProfile);
				NPC.rotation += 0.3f * NPC.direction;
				NPC.Opacity = Math.Min(NPC.Opacity + 0.1f, 1);
				NPC.velocity.X *= 0.9f;

				if (Counter > 10)
				{
					NPC.noTileCollide = false;

					if (Grounded) //Land
					{
						ChangeState(Main.rand.NextFromList(Walking, BounceGroundPound));
						SetFrame(DigFrame, PhaseOneProfile);

						return;
					}
				}

				break;
		}
	}
	#endregion

	#region Phase 2
	public void FlyHover()
	{
		const int hover_time = 180;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0.7f;
		NPC.rotation = NPC.velocity.X * 0.05f;
		NPC.FaceTarget();

		UpdateFrame(2, 12, PhaseTwoProfile);

		float heightAboveGround = FindGroundFromPosition(NPC.Center).Y - NPC.Center.Y;

		//Vertical movement
		if (heightAboveGround < 128)
			NPC.velocity.Y -= 0.1f;

		else if (Math.Abs(NPC.position.Y - Target.position.Y) > 160)
			NPC.velocity.Y -= 0.175f * Math.Sign(NPC.Center.Y - Target.Center.Y);
		else
			NPC.velocity.Y *= 0.9f;

		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * Counter / hover_time) / 10;

		//Horizontal movement

		if (NPC.Center.X < Target.Center.X)
		{
			if (NPC.velocity.X < 0)
			{
				NPC.velocity.X *= 0.975f;
				NPC.velocity.X += 0.025f;
			}
			else
			{
				NPC.velocity.X += 0.1f;
			}
		}

		else
		{
			if (NPC.velocity.X > 0)
			{
				NPC.velocity.X *= 0.975f;
				NPC.velocity.X -= 0.025f;
			}
			else
			{
				NPC.velocity.X -= 0.1f;
			}
		}

		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -12, 12);
		NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -8, 8);

		if (Counter > hover_time)
			ChangeState(SelectWeightedState());
	}

	public void FlyingDash()
	{
		const int expire_time = 300;
		const int idle_time = 90;
		const int dash_time = 30;

		ref float dashRotation = ref NPC.ai[2];
		ref float dashState = ref NPC.ai[3];

		UpdateFrame(2, 12, PhaseTwoProfile);
		NPC.noTileCollide = true;
		dealContactDamage = true;
		bool inRange = NPC.DistanceSQ(Target.Center) < 350 * 350;

		if ((inRange || dashState == 1) && Counter > idle_time)
		{
			if (dashState == 0)
			{
				dashRotation = NPC.AngleTo(Target.Center);
				dashState = 1;
				Counter = idle_time + 1;
			}

			SetFrame(0, 2, PhaseTwoProfile);
			showTrail = true;

			NPC.velocity = Vector2.UnitX.RotatedBy(dashRotation) * 18;
			NPC.direction = Math.Sign(NPC.velocity.X);
			NPC.rotation = NPC.velocity.ToRotation() + ((NPC.direction == -1) ? MathHelper.Pi : 0);

			if (Counter > idle_time + dash_time)
			{
				ChangeState(FlyHover);
				NPC.velocity *= 0.4f;
			}
		}
		else
		{
			NPC.FaceTarget();
			Vector2 targetPosition = Target.Center + new Vector2(300, 0) * -NPC.direction;

			NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(targetPosition) * 5, 0.05f);
			NPC.rotation = NPC.velocity.X * 0.05f;
		}

		if (Counter > expire_time)
		{
			NPC.noTileCollide = false;
			dealContactDamage = false;
			ChangeState(FlyHover);
		}
	}

	public void ChainGroundPound()
	{
		const int expire_time = 300;
		const int idle_time = 90;

		ref float jumpState = ref NPC.ai[2];

		if (jumpState > 3) //Final bounce
		{
			if (currentFrame == DigFrame) //One-time effects
				SetFrame(3, PhaseTwoProfile.GetFrameCount(3) - 1, PhaseTwoProfile);

			NPC.velocity *= 0.95f;
			NPC.rotation = 0;

			if (UpdateFrame(3, -12, PhaseTwoProfile, false) == FrameState.Stopped)
				ChangeState(FlyHover);
		}
		else if (jumpState == 0) //Line up with Target
		{
			float distance = NPC.DistanceSQ(Target.Center);
			Vector2 targetPosition = Target.Center - new Vector2(0, 200);

			NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(targetPosition) * 5, 0.04f * (distance / (200f * 200f)));
			NPC.rotation = NPC.velocity.X * 0.05f;

			if (distance < 250 * 250 && Counter > idle_time)
			{
				Counter = 0;
				NPC.velocity.Y -= 5;
				jumpState++;
			}

			UpdateFrame(2, 12, PhaseTwoProfile);

			if (Math.Abs(Target.Center.X - NPC.Center.X) > 50)
				NPC.FaceTarget();

			NPC.noTileCollide = true;
		}
		else //Fall
		{
			if (Profile == PhaseOneProfile)
				SetFrame(DigFrame, PhaseOneProfile);
			else if (UpdateFrame(3, 12, PhaseTwoProfile, false) == FrameState.Stopped)
				Profile = PhaseOneProfile;

			if (Grounded) //Collide
			{
				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 5, 9, 15));

				Counter = 0;
				Profile = PhaseOneProfile;
				NPC.velocity.Y = -18; //Bounce up
				NPC.FaceTarget();

				jumpState++;
			}

			NPC.noTileCollide = false;
			NPC.velocity.Y += 0.5f;
			NPC.velocity.X = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(Target.Center) * 6, 0.1f).X; //Track Target

			if (currentFrame == DigFrame)
				NPC.rotation += 0.1f * NPC.direction;
		}

		if (Counter > expire_time)
			ChangeState(FlyHover);
	}

	public void LeapDig()
	{
		const int underground_time = 180;
		const int num_eruptions = 3;
		const int rest_time = 40;

		ref float jumpState = ref NPC.ai[2];
		ref float groundState = ref NPC.ai[3];

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0;
		NPC.behindTiles = true;

		if (jumpState == 0) //Jump up
		{
			jumpState++;
			NPC.velocity = new Vector2(6 * NPC.direction, -12);
		}

		if (groundState == 0 && jumpState == 1) //Fall into the ground
		{
			if (Profile != PhaseOneProfile && UpdateFrame(3, 12, PhaseTwoProfile, false) == FrameState.Stopped)
				SetFrame(DigFrame, PhaseOneProfile);

			NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.4f, 24);

			if (currentFrame == DigFrame)
				NPC.rotation += MathHelper.Clamp(NPC.velocity.Y * 0.05f * NPC.direction, -1, 1);

			if (Collision.SolidCollision(NPC.Top - new Vector2(4), 8, 8)) //Disappear into the ground
			{
				groundState = 1;
				NPC.Opacity = 0;
				NPC.velocity = Vector2.Zero;

				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 4, 3, 20));

				//fx here
			}
		}

		if (groundState == 1) //Dig
		{
			Vector2 groundPosition = FindGroundFromPosition(new Vector2(NPC.Center.X, Target.Center.Y));
			NPC.velocity.X = (float)Math.Sin(Counter * MathHelper.TwoPi / 120) * 5 + NPC.DirectionTo(Target.Center).X;
			NPC.position.Y = groundPosition.Y;

			if (Main.rand.NextBool(4) && !Main.dedServ)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Center - Vector2.UnitY * 32, -Vector2.UnitY * 8, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.1f, 0.25f), EaseFunction.EaseCubicOut, 30)
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.5f,
					Layer = ParticleLayer.BelowSolid
				});
			}

			if (Counter % (underground_time / (num_eruptions + 1)) == 0 && Counter != underground_time)
			{
				//projectile here					
				Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center - Vector2.UnitY * 80, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3);

				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 2, 3, 20));
			}

			if (Counter % 20 == 0)
				BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX);

			if (Counter > underground_time) //Reemerge
			{
				SetFrame(0, 2, PhaseOneProfile);
				Counter = 0;

				NPC.FaceTarget();
				NPC.rotation = 0;
				NPC.Opacity = 1;
				NPC.velocity.Y = -15;

				groundState = 0;
				jumpState++;

				//fx here

				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 4, 3, 20));
			}
		}

		if (jumpState == 2)
		{
			Counter++;
			NPC.velocity.Y *= 0.95f;

			if (Counter > rest_time)
			{
				Profile = PhaseTwoProfile;
				ChangeState(SelectWeightedState());
			}
		}
	}

	public void ScarabSwarm()
	{
		//durations of each segment
		const int attack_start_time = 40;
		const int swarm_length = 120;
		const int flash_boom_chargeup = 60;
		const int rest_time = 30;

		//short form calculations using durations of each segment
		int flashChargeStart = attack_start_time + swarm_length;
		int flashExplostionTime = attack_start_time + swarm_length + flash_boom_chargeup;
		int attackEndTime = attack_start_time + swarm_length + flash_boom_chargeup + rest_time;

		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.knockBackResist = 0f;

		//attack start
		if (Counter == 0)
		{
			NPC.velocity.Y = -6;
			NPC.velocity.X /= 2;
			//fx here?
		}

		NPC.velocity *= 0.95f;
		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * 3 * Counter / attackEndTime) / 10;
		UpdateFrame(1, 12, PhaseTwoProfile);

		if (Counter == attack_start_time)
		{
			//proj here

			if (Main.netMode != NetmodeID.Server)
			{
				ParticleHandler.SpawnParticle(new TexturedPulseCircle(NPC.Center, Color.LightGoldenrodYellow, 1, 2400, 30, "GlowTrail", new Vector2(1, 1), EaseFunction.EaseCircularOut, true, 0.33f));
			}
		}

		if (Counter > flashChargeStart && Counter < flashExplostionTime)
		{
			//vfx here
		}

		if (Counter == flashExplostionTime)
		{
			if (Main.netMode != NetmodeID.Server)
			{
				for (int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new DissipatingImage(NPC.Center, Color.Lerp(Color.LightGoldenrodYellow, Color.Goldenrod, 0.5f).Additive(), Main.rand.NextFloatDirection(), 0.66f, 0, "GodrayCircle", Vector2.Zero, new Vector2(3, 1.4f), 15));

			}
		}

		if (Counter > attackEndTime)
			ChangeState(SelectWeightedState());
	}
	#endregion
}