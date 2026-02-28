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
		const int swarmTime = 120;
		const int roarTime = 120;

		/*Todo: 
		 * foreground scarab particles fly across the screen from bottom left to top right
		 * screenshake
		 * ground beneath player starts emitting particles
		 * scarab bursts out of ground and roars
		*/

		if (Counter == 0 && !Main.dedServ)
		{
			if (Main.LocalPlayer.Distance(Target.Center) < 800)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Target.Center, Vector2.UnitX, 0.5f, 3, swarmTime * 2));

			for (int i = 0; i < 48; i++)
			{
				Vector2 scarabPos = Target.Center;
				bool backgroundScarab = !Main.rand.NextBool(3);
				int spawnDelayRange = (int)(swarmTime * (backgroundScarab ? 0.25f : 0.66f));
				int spawnDelayStatic = backgroundScarab ? 0 : swarmTime / 3;
				scarabPos += new Vector2(-Main.rand.NextFloat(900, 1400), Main.rand.NextFloat(200, 800)) * (backgroundScarab ? 1f : 1.2f);
				ParticleHandler.SpawnQueuedParticle(new ScarabParticle(scarabPos, Main.rand.NextFloat(0.3f, 0.7f), 1, backgroundScarab), Main.rand.Next(spawnDelayRange) + spawnDelayStatic);
			}
		}

		if (Counter == swarmTime)
		{
			NPC.Center = FindGroundFromPosition(Target.Center);
			NPC.noTileCollide = false;
			NPC.noGravity = false;
			NPC.velocity.Y = -12;
			NPC.Opacity = 1;
			_inGround = false;
		}

		if (Counter >= swarmTime + roarTime)
		{
			NPC.dontTakeDamage = false;
			ChangeState(Walking);
		}
	}
	#endregion

	#region Phase 1
	public void Walking()
	{
		int maxWalkTime = 360;
		int maxBoredom = 60;

		NPC.knockBackResist = 0.7f;
		CheckPlatform();
		NPC.FaceTarget();

		if (NPC.velocity == Vector2.Zero)
		{
			DigTimer++;

			if (DigTimer > 30) // If stuck for a half second, leap
			{
				ChangeState(Dig);
				return;
			}
		}

		//Check if grounded
		if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
		{
			if (DetermineGap()) // Jump over gaps if needed
			{
				JumpTimer++;

				if (JumpTimer > 15)
				{
					_escapeJump = true;
					ChangeState(Leap);
					return;
				}
			}

			//Only move if too far from the player, try to move away a little bit if too close

			float horizontalDist = Math.Abs(NPC.position.X - Target.position.X);
			if (horizontalDist > 200 || Counter < 30)
			{
				NPC.velocity.X += NPC.direction * 0.3f;
				_boredomTimer = Math.Max(_boredomTimer - 1, 0);
			}
			else
			{
				if (Math.Sign(NPC.velocity.X) == NPC.direction && Math.Abs(NPC.velocity.X) > 2)
					NPC.velocity.X -= NPC.direction * 0.1f;

				if (horizontalDist < 140)
				{
					_boredomTimer++;

					if (_boredomTimer > 2 * maxBoredom / 3)
						NPC.velocity.X -= NPC.direction * 0.1f;
				}
			}
		}

		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -5, 5);

		float fps = Math.Max(NPC.velocity.X * 2, 8) * NPC.direction;
		UpdateFrame(1, (int)fps);

		if (_boredomTimer >= maxBoredom)
			ChangeState(Main.rand.NextFromList(Skitter, HornSwipe));

		/*
		 * Todo:
		 * Adjust movement to feel more natural
		 * Leap if too far or can't traverse terrain and a leap would reach the player (Pits, height differences)
		 * Dig if too far or can't traverse terrain and a leap wouldn't reach player (Collision)
		 */

		if (Counter > maxWalkTime)
			ChangeState(SelectRandomState());
	}

	/// <summary>
	/// Determines if this NPC, while moving, is approaching a gap that requires jumping over.
	/// </summary>
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
		const int windupTime = 30;
		const int attackTime = 35;

		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform();

		NPC.velocity.X *= 0.5f;

		if (Counter < windupTime)
			UpdateFrame(2, (int)(3 * 60f / windupTime), false);
		else
			UpdateFrame(2, (int)(7 * 60f / attackTime), false);

		if (currentFrame.Y is > 2 and < 7)
			_contactDmgEnabled = true;

		if (Counter++ >= attackTime + windupTime)
			ChangeState(SelectRandomState());
	}

	public void Skitter()
	{
		const int skitterTime = 40;

		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform();

		NPC.velocity.X = -NPC.direction * MathHelper.Lerp(12, 4, EaseFunction.EaseQuadOut.Ease(Counter / skitterTime));
		Counter++;
		UpdateFrame(1, (int)(NPC.direction * NPC.velocity.X) * 4);

		if (Counter > skitterTime)
			ChangeState(SelectRandomState());
	}

	public void Leap()
	{
		const int windupTime = 40;
		const int restTime = 45;

		bool HasJumped = _jumpState == 1;
		bool HasLanded = _jumpState == 2;

		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform();

		if (!HasJumped && !HasLanded)
		{
			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				//Slow down for a bit, then calculate mortar velocity to jump towards player
				//Increase velocity if too far to reach player

				if (Counter <= windupTime)
				{
					NPC.velocity.X *= 0.8f;
					UpdateFrame(3, (int)(6 * windupTime / 60f), false);
					NPC.FaceTarget();

					if (Counter == windupTime)
					{
						Vector2 desiredPos = Target.Center + Target.velocity * 6 + NPC.direction * 112 * Vector2.UnitX;
						NPC.velocity = NPC.GetArcVel(desiredPos, 0.38f, 15, true);
						//NPC.noTileCollide = true;
						_jumpState++;
						NPC.netUpdate = true;
					}
				}
			}
		}
		else if (!HasLanded)
		{
			currentFrame = new(4, 5);
			_contactDmgEnabled = true;

			if (NPC.velocity.Y < 0)
				NPC.noTileCollide = true;
			else
				NPC.noTileCollide = false;

			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y > 0)
			{
				_jumpState++;
				//vfx and sfx and shockwaves here
				NPC.netUpdate = true;
				Counter = 0;
			}
		}
		else
		{
			NPC.velocity.X = 0;
			UpdateFrame(4, (int)(15 * restTime / 60f), false);

			if (Counter > restTime)
				ChangeState(SelectRandomState());
		}

		/*
		 * Todo:
		 * Phase through some tiles but avoid phasing through a wall, use primarily for closing vertical gaps, pits, and walls
		 */
	}

	public void RollDash()
	{
		const int windupTime = 120;
		const int dashTime = 50;
		const int transitionTime = 40;

		NPC.noTileCollide = false;
		NPC.noGravity = false;

		CheckPlatform();

		if (Counter < windupTime)
		{
			NPC.FaceTarget();
			UpdateFrame(1, -10);

			if (Counter > windupTime / 1.1f)
			{
				NPC.velocity.X += NPC.direction;
			}
			else if (Counter > windupTime / 1.5f)
			{
				NPC.velocity.X *= 0.8f;
				currentFrame = new(0, 0);
			}
			else
			{
				NPC.velocity.X = NPC.direction * -(1f - (float)Counter / windupTime) * 3;
			}
		}
		else if (Counter == windupTime)
		{
			NPC.velocity.X = NPC.direction * 28;
			currentFrame = new(0, 4);
			//sfx and vfx here
		}

		if (Counter >= windupTime + dashTime + transitionTime)
		{
			//end attack
			NPC.velocity.X /= 2;
			ChangeState(Walking);
		}
		else if (Counter >= windupTime + dashTime)
		{
			//skid to a stop
			currentFrame = new(0, 1);
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
		else if (Counter > windupTime)
		{
			NPC.Step();

			_contactDmgEnabled = true;
			NPC.rotation += 0.2f * NPC.spriteDirection;
			NPC.velocity.X *= 0.98f;
			//sfx here

			//if (NPC.collideX)
			//	ChangeState(BounceGroundPound); //bounce off of surfaces
		}
	}

	public void GroundedSlam()
	{
		const int duration = 90;

		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		CheckPlatform();

		//Flip direction only on first frame
		if (Counter == 0)
			NPC.FaceTarget();

		UpdateFrame(7, (int)(Profile.FrameCount[7] * 60f / duration));

		if (Counter == (int)(duration * 0.6f))
		{
			_contactDmgEnabled = true;
			//projectiles and sfx here

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 center = FindGroundFromPosition(NPC.Center + NPC.direction * Vector2.UnitX * 320);
				Projectile.NewProjectile(NPC.GetSource_FromThis(), center - Vector2.UnitY * 160, Vector2.Zero, ModContent.ProjectileType<SlamShockwave>(), NPC.damage / 2, 16, Main.myPlayer, NPC.direction);
			}
		}

		if (Counter > duration)
			ChangeState(SelectRandomState());
	}

	public void BounceGroundPound()
	{
		const int maxBounces = 3;
		const int finalBounceTrackTime = 40;
		const int airPauseTime = 20;
		const int restTime = 90;

		bool isGravityAllowed = true;

		NPC.noTileCollide = false;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;
		CheckPlatform();

		if (_jumpState < maxBounces)
		{
			currentFrame = new(0, 4);
			_contactDmgEnabled = true;

			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				_jumpState++;
				NPC.velocity.Y = -16;
				NPC.FaceTarget();

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 15));
			}
			else
			{
				BounceTracking();
			}
		}
		else if (_jumpState == maxBounces)
		{
			_contactDmgEnabled = true;

			//Continue tracking in the air for a bit
			if (Counter < finalBounceTrackTime)
			{
				BounceTracking();

				if (Counter > finalBounceTrackTime - 10)
					NPC.velocity.X *= 0.9f;
			}
			else
			{
				NPC.rotation += NPC.direction * 0.3f;

				if (Counter < finalBounceTrackTime + airPauseTime)
				{
					isGravityAllowed = false;
					NPC.velocity.X = 0;
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -1.25f, 0.3f);
				}

				else if (Counter == finalBounceTrackTime + airPauseTime)
					NPC.velocity.Y = 16;
			} //Pause and spin faster in air and slam down

			//On tile collision
			if (Counter > finalBounceTrackTime + airPauseTime && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				_jumpState++; //use the variable to track the final ground pound too
				NPC.velocity.Y = -4;
				NPC.Step();

				for (int i = -3; i <= 3; i++)
				{
					if (i == 0)
						continue;

					float distStep = Main.rand.NextFloat(11, 13) * i * 16;
					Vector2 projPosition = FindGroundFromPosition(NPC.Bottom + Vector2.UnitX * distStep) - Vector2.UnitY * 80;

					Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3, Main.myPlayer, Math.Abs(i) * 40);
				}

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 15));
			}
		}
		else //rest before next attack
		{
			UpdateFrame(4, (int)(8 * restTime / 60f), false);

			if (currentFrame.Y < 7)
				currentFrame.Y = 7;

			NPC.rotation = 0;
			isGravityAllowed = false;
			NPC.noGravity = false;

			if (Counter > finalBounceTrackTime + airPauseTime + restTime)
				ChangeState(SelectRandomState());
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
		const int digStartTime = 60;
		const int undergroundTime = 180;
		const int airTime = 40;

		NPC.behindTiles = true;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;

		if (Counter < digStartTime)
		{
			//dig into ground anim here, placeholder rn
			NPC.velocity = Vector2.Zero;
			NPC.position.Y += 0.5f;
			UpdateFrame(3, 6, false);
		}
		else if (Counter == digStartTime)
		{
			//temp for hiding boss
			_inGround = true;
			NPC.alpha = 255;
			NPC.Center = FindGroundFromPosition(Target.Center);

			if (Collision.SolidCollision(Target.position - new Vector2(40), Target.width + 80, Target.height + 30))
				_escapeJump = true;
		}
		else if (Counter < undergroundTime + digStartTime)
		{
			//set npc's position to tiles under player, moving around left and right, before settling on a position
			//particles spawn from the tile where the npc is located

			NPC.FaceTarget();

			NPC.noGravity = true;
			NPC.noTileCollide = true;

			float desiredVel = NPC.DirectionTo(Target.Center).X * 8;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);
			NPC.position.Y = FindGroundFromPosition(NPC.position).Y;
			currentFrame = new(0, 4);

			if (!Main.dedServ)
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
					BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX + NPC.velocity / 2);
			}
		}
		else if (Counter == undergroundTime + digStartTime)
		{
			//pop out of ground here
			_inGround = false;
			NPC.alpha = 0;
			NPC.rotation = MathHelper.PiOver4;
			NPC.velocity.X *= 0.3f;
			NPC.velocity.Y = _escapeJump ? -12 : -16;

			if (_escapeJump && Main.netMode != NetmodeID.MultiplayerClient)
			{
				//NPC.velocity.X = Main.rand.NextBool() ? -9 : 9;
				NPC.netUpdate = true;
			}

			_escapeJump = false;
		}
		else if (Counter < undergroundTime + digStartTime + airTime)
		{
			if (NPC.noTileCollide && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height)) // Only re-enable collision when not in tiles
				NPC.noTileCollide = false;

			NPC.noGravity = false;
			_contactDmgEnabled = true;
			NPC.rotation += NPC.direction * 0.125f;

			//curl anim here
		}
		else
		{
			NPC.noTileCollide = false;
			ChangeState(BounceGroundPound);
		}
	}
	#endregion

	#region Phase 2
	public void FlyHover()
	{
		const int hoverTime = 180;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0.7f;

		UpdateFrame(1, 12, true);

		float heightAboveGround = FindGroundFromPosition(NPC.Center).Y - NPC.Center.Y;

		//Vertical movement
		if (heightAboveGround < 128)
			NPC.velocity.Y -= 0.1f;

		else if (Math.Abs(NPC.position.Y - Target.position.Y) > 160)
			NPC.velocity.Y -= 0.175f * Math.Sign(NPC.Center.Y - Target.Center.Y);

		else
			NPC.velocity.Y *= 0.9f;

		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * Counter / hoverTime) / 10;

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

		if (Counter > hoverTime)
			ChangeState(SelectRandomState());
	}

	public void FlyingDash()
	{
		const int expire_time = 300;
		const int idle_time = 90;
		const int dash_time = 30;

		UpdateFrame(2, 12);
		bool inRange = NPC.DistanceSQ(Target.Center) < 250 * 250;

		if ((inRange || _dashDirection != default) && Counter > idle_time)
		{
			if (_dashDirection == default)
			{
				_dashDirection = NPC.DirectionTo(Target.Center);
				Counter = idle_time + 1;
			}

			currentFrame = new(0, 2);
			NPC.noTileCollide = true;
			NPC.velocity = _dashDirection * 18;
			NPC.direction = Math.Sign(NPC.velocity.X);
			NPC.rotation = NPC.velocity.ToRotation() + ((NPC.direction == -1) ? MathHelper.Pi : 0);

			if (Counter > idle_time + dash_time)
			{
				_dashDirection = default;
				NPC.noTileCollide = false;

				ChangeState(FlyHover);
			}
		}
		else
		{
			int direction = Math.Sign(NPC.DirectionTo(Target.Center).X);
			Vector2 targetPosition = Target.Center + new Vector2(200, 0) * -direction;

			NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(targetPosition) * 5, 0.05f);
			NPC.rotation = NPC.velocity.X * 0.05f;
			NPC.direction = direction;
		}

		if (Counter > expire_time)
		{
			_dashDirection = default;
			NPC.noTileCollide = false;

			ChangeState(FlyHover);
		}
	}

	public void ChainGroundPound()
	{
		const int expire_time = 300;
		const int idle_time = 90;
		const int prepare_time = 30;
		const int recover_time = 30;

		ref float jumpState = ref NPC.ai[2];
		bool inRange = NPC.DistanceSQ(Target.Center) < 250 * 250;

		int direction = Math.Sign(NPC.DirectionTo(Target.Center).X);
		NPC.direction = direction;
		UpdateFrame(2, 12);

		switch (jumpState % 3)
		{
			case 2: //Recover
				{
					if (Counter > recover_time)
					{
						if (++jumpState / 3 > 2)
							ChangeState(FlyHover);

						_dashDirection = default;
						Counter = 0;
					}

					NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0, -3), 0.1f);
					break;
				}
			case 1: //Prepare/Pound
				{
					if (Counter > prepare_time) //Pound
					{
						if (NPC.velocity.Y == 0) //Collide
						{
							if (!Main.dedServ)
								Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 5, 9, 15));

							jumpState++;
							Counter = 0;
						}

						currentFrame = new(0, 2);
						NPC.velocity = _dashDirection * 30;

						NPC.noTileCollide = false;
						NPC.rotation = NPC.velocity.ToRotation() + ((NPC.direction == -1) ? MathHelper.Pi : 0);
					}
					else
					{
						NPC.velocity.Y = Math.Max(-Math.Abs(NPC.velocity.Y * 1.2f), -10); //Quickly ascend before pounding
					}

					break;
				}
			case 0: //Hover over Target
				{
					Vector2 targetPosition = Target.Center - new Vector2(0, 200);

					NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(targetPosition) * 5, 0.1f);
					NPC.rotation = NPC.velocity.X * 0.05f;

					if (inRange && Counter > idle_time)
					{
						_dashDirection = Vector2.UnitY;
						Counter = 0;

						jumpState++;
					}

					break;
				}
		}

		if (Counter > expire_time)
			ChangeState(FlyHover);
	}

	public void LeapDig()
	{
		const int undergroundTime = 180;
		const int numEruptions = 3;
		const int restTime = 40;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;

		if (_jumpState == 0)
		{
			_jumpState++;
			NPC.velocity = new Vector2(6 * NPC.direction, -12);
			NPC.netUpdate = true;
		}

		if (!_inGround && _jumpState == 1)
		{
			NPC.velocity.Y += 0.4f;
			NPC.velocity.Y = Math.Min(NPC.velocity.Y, 24);

			if (Collision.SolidTiles(NPC.position, NPC.width, NPC.height))
			{
				_inGround = true;
				NPC.Opacity = 0;
				NPC.velocity = Vector2.Zero;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));

				//fx here
			}
		}

		if (_inGround)
		{
			NPC.velocity.X = (float)Math.Sin(Counter * MathHelper.TwoPi / 120) * 5 + NPC.DirectionTo(Target.Center).X;
			NPC.position.Y = FindGroundFromPosition(NPC.position).Y;

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

			if (Counter % (undergroundTime / (numEruptions + 1)) == 0 && Counter != undergroundTime)
			{
				//projectile here					
				Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center - Vector2.UnitY * 80, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3);

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 20));
			}

			if (Counter > undergroundTime)
			{
				_inGround = false;
				NPC.Opacity = 1;
				NPC.velocity.Y = -15;
				Counter = 0;
				_jumpState++;

				//fx here
				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));
			}
		}

		if (_jumpState == 2)
		{
			Counter++;
			NPC.velocity.Y *= 0.95f;

			if (Counter > restTime)
				ChangeState(SelectRandomState());
		}
	}

	public void ScarabSwarm()
	{
		//durations of each segment
		const int attackStartTime = 40;
		const int swarmLength = 120;
		const int flashBoomChargeup = 60;
		const int restTime = 30;

		//short form calculations using durations of each segment
		int flashChargeStart = attackStartTime + swarmLength;
		int flashExplostionTime = attackStartTime + swarmLength + flashBoomChargeup;
		int attackEndTime = attackStartTime + swarmLength + flashBoomChargeup + restTime;

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

		if (Counter == attackStartTime)
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
			ChangeState(SelectRandomState());
	}
	#endregion
}