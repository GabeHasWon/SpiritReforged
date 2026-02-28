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

		AITimer++;

		if (AITimer == 1 && !Main.dedServ)
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

		if (AITimer == swarmTime)
		{
			NPC.Center = FindGroundFromPosition(Target.Center);
			NPC.noTileCollide = false;
			NPC.noGravity = false;
			NPC.velocity.Y = -12;
			NPC.Opacity = 1;
			_inGround = false;
		}

		if (AITimer >= swarmTime + roarTime)
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

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0.7f;
		AITimer++;
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
			if (horizontalDist > 200 || AITimer < 30)
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
		NPC.Step();

		if (_boredomTimer >= maxBoredom)
			ChangeState(Main.rand.NextFromList(Skitter, HornSwipe));

		/*
		 * Todo:
		 * Adjust movement to feel more natural
		 * Leap if too far or can't traverse terrain and a leap would reach the player (Pits, height differences)
		 * Dig if too far or can't traverse terrain and a leap wouldn't reach player (Collision)
		 */

		if (AITimer > maxWalkTime)
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

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform();

		NPC.velocity.X *= 0.5f;

		if (AITimer < windupTime)
			UpdateFrame(2, (int)(3 * 60f / windupTime), false);
		else
			UpdateFrame(2, (int)(7 * 60f / attackTime), false);

		if (currentFrame.Y is > 2 and < 7)
			_contactDmgEnabled = true;

		if (AITimer++ >= attackTime + windupTime)
			ChangeState(SelectRandomState());
	}

	public void Skitter()
	{
		const int skitterTime = 40;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform();

		NPC.velocity.X = -NPC.direction * MathHelper.Lerp(12, 4, EaseFunction.EaseQuadOut.Ease(AITimer / skitterTime));
		AITimer++;
		UpdateFrame(1, (int)(NPC.direction * NPC.velocity.X) * 4);

		if (AITimer > skitterTime)
			ChangeState(SelectRandomState());
	}

	public void Leap()
	{
		const int windupTime = 40;
		const int restTime = 45;

		bool HasJumped = _jumpState == 1;
		bool HasLanded = _jumpState == 2;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform();

		if (!HasJumped && !HasLanded)
		{
			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				AITimer++;
				//Slow down for a bit, then calculate mortar velocity to jump towards player
				//Increase velocity if too far to reach player

				if (AITimer <= windupTime)
				{
					NPC.velocity.X *= 0.8f;
					UpdateFrame(3, (int)(6 * windupTime / 60f), false);
					NPC.FaceTarget();

					if (AITimer == windupTime)
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
				AITimer = 0;
			}
		}
		else
		{
			NPC.velocity.X = 0;
			UpdateFrame(4, (int)(15 * restTime / 60f), false);

			AITimer++;

			if (AITimer > restTime)
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

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = false;

		CheckPlatform();
		AITimer++;

		if (AITimer < windupTime)
		{
			NPC.FaceTarget();
			UpdateFrame(1, -10);

			if (AITimer > windupTime / 1.1f)
			{
				NPC.velocity.X += NPC.direction;
			}
			else if (AITimer > windupTime / 1.5f)
			{
				NPC.velocity.X *= 0.8f;
				currentFrame = new(0, 0);
			}
			else
			{
				NPC.velocity.X = NPC.direction * -(1f - (float)AITimer / windupTime) * 3;
			}
		}
		else if (AITimer == windupTime)
		{
			NPC.velocity.X = NPC.direction * 28;
			currentFrame = new(0, 4);
			//sfx and vfx here
		}

		if (AITimer >= windupTime + dashTime + transitionTime)
		{
			//end attack
			NPC.velocity.X /= 2;
			ChangeState(Walking);
		}
		else if (AITimer >= windupTime + dashTime)
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
		else if (AITimer > windupTime)
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

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		CheckPlatform();

		//Flip direction only on first frame
		if (AITimer++ == 0)
			NPC.FaceTarget();

		UpdateFrame(7, (int)(Profile.FrameCount[7] * 60f / duration));

		if (AITimer == (int)(duration * 0.6f))
		{
			_contactDmgEnabled = true;
			//projectiles and sfx here

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 center = FindGroundFromPosition(NPC.Center + NPC.direction * Vector2.UnitX * 320);
				Projectile.NewProjectile(NPC.GetSource_FromThis(), center - Vector2.UnitY * 160, Vector2.Zero, ModContent.ProjectileType<SlamShockwave>(), NPC.damage / 2, 16, Main.myPlayer, NPC.direction);
			}
		}

		if (AITimer > duration)
			ChangeState(SelectRandomState());
	}

	public void BounceGroundPound()
	{
		const int maxBounces = 3;
		const int finalBounceTrackTime = 40;
		const int airPauseTime = 20;
		const int restTime = 90;

		bool isGravityAllowed = true;

		NPC.spriteDirection = NPC.direction;
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
			AITimer++;
			_contactDmgEnabled = true;

			//Continue tracking in the air for a bit
			if (AITimer < finalBounceTrackTime)
			{
				BounceTracking();

				if (AITimer > finalBounceTrackTime - 10)
					NPC.velocity.X *= 0.9f;
			}
			else
			{
				NPC.rotation += NPC.direction * 0.3f;

				if (AITimer < finalBounceTrackTime + airPauseTime)
				{
					isGravityAllowed = false;
					NPC.velocity.X = 0;
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -1.25f, 0.3f);
				}

				else if (AITimer == finalBounceTrackTime + airPauseTime)
					NPC.velocity.Y = 16;
			} //Pause and spin faster in air and slam down

			//On tile collision
			if (AITimer > finalBounceTrackTime + airPauseTime && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
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
			AITimer++;
			isGravityAllowed = false;
			NPC.noGravity = false;

			if (AITimer > finalBounceTrackTime + airPauseTime + restTime)
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
		NPC.spriteDirection = NPC.direction;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		AITimer++;

		if (AITimer < digStartTime)
		{
			//dig into ground anim here, placeholder rn
			NPC.velocity = Vector2.Zero;
			NPC.position.Y += 0.5f;
			UpdateFrame(3, 6, false);
		}
		else if (AITimer == digStartTime)
		{
			//temp for hiding boss
			_inGround = true;
			NPC.alpha = 255;
			NPC.Center = FindGroundFromPosition(Target.Center);

			if (Collision.SolidCollision(Target.position - new Vector2(40), Target.width + 80, Target.height + 30))
				_escapeJump = true;
		}
		else if (AITimer < undergroundTime + digStartTime)
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

				if (AITimer % 20 == 0)
					BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX + NPC.velocity / 2);
			}
		}
		else if (AITimer == undergroundTime + digStartTime)
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
		else if (AITimer < undergroundTime + digStartTime + airTime)
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
		AITimer++;

		UpdateFrame(1, 12, true);

		float heightAboveGround = FindGroundFromPosition(NPC.Center).Y - NPC.Center.Y;

		//Vertical movement
		if (heightAboveGround < 128)
			NPC.velocity.Y -= 0.1f;

		else if (Math.Abs(NPC.position.Y - Target.position.Y) > 160)
			NPC.velocity.Y -= 0.175f * Math.Sign(NPC.Center.Y - Target.Center.Y);

		else
			NPC.velocity.Y *= 0.9f;

		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * AITimer / hoverTime) / 10;

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

		if (AITimer > hoverTime)
			ChangeState(SelectRandomState());
	}

	public void FlyingDash()
	{
		const int prepTime = 90;
		const int dashTime = 70;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;
		AITimer++;

		UpdateFrame(2, 12, true);

		if (AITimer < prepTime)
		{
			//vertical

			if (Math.Abs(NPC.position.Y - Target.position.Y) > 32)
				NPC.velocity.Y -= 0.25f * Math.Sign(NPC.Center.Y - Target.Center.Y);

			else
				NPC.velocity.Y *= 0.9f;

			NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * AITimer / prepTime) / 10;

			//horizontal

			float desiredPos = Target.Center.X - 132 * (NPC.Center.X < Target.Center.X ? 1 : -1);

			if (Math.Abs(NPC.Center.X - desiredPos) > 48)
			{
				if (NPC.Center.X < desiredPos)
					NPC.velocity.X += 0.2f;
				else
					NPC.velocity.X -= 0.2f;
			}
			else
			{
				NPC.velocity.X *= 0.9f;
			}

			float windupThreshold = 0.66f;
			if (AITimer > prepTime * windupThreshold)
				NPC.velocity.X -= NPC.direction * MathHelper.Lerp(0.25f, 1f, EaseFunction.EaseQuadOut.Ease((AITimer - prepTime * windupThreshold) / (prepTime * (1 - windupThreshold))));

			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -12, 12);
			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -8, 8);
		}

		if (AITimer == prepTime)
		{
			NPC.velocity.X = (NPC.Center.X > Target.Center.X ? -1 : 1) * 34;
			NPC.velocity.Y /= 3;
			// fx here
		}

		if (AITimer > prepTime)
		{
			NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
			NPC.velocity.X = MathHelper.Lerp(34 * NPC.direction, 0, EaseFunction.EaseCubicOut.Ease((AITimer - prepTime) / dashTime));
		}

		if (AITimer > prepTime + dashTime)
			ChangeState(SelectRandomState());
	}

	public void ChainGroundPound()
	{
		const int maxBounces = 2;
		const int maxPounds = 3;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = true; //for some godforsaken reason this also force caps an npc's downwards velocity to 10
		NPC.knockBackResist = 0f;
		CheckPlatform();
		currentFrame = new(0, 1);

		if (_jumpState < maxBounces)
		{
			_contactDmgEnabled = true;

			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				_jumpState++;
				NPC.velocity.Y = -16;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 15));
			}
			else
			{
				float desiredVel = (NPC.Center.X < Target.Center.X) ? 16 : -16;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);

				if (NPC.velocity.Y < 12)
					NPC.velocity.Y += 0.08f;

				NPC.rotation += NPC.velocity.X / 120;
			}

			NPC.velocity.Y += 0.38f;
		}
		else if (_jumpState is >= maxBounces and < (maxBounces + maxPounds))
		{
			NPC.knockBackResist = 0f;
			AITimer++;
			currentFrame.Y = 2;
			_contactDmgEnabled = true;

			if (AITimer < 25)
			{
				float desiredVel = (NPC.Center.X < Target.Center.X) ? 24 : -24;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);

				if (_jumpState == maxBounces)
				{
					currentFrame.Y = 1;
					NPC.rotation += NPC.velocity.X / 120;
				}

				if (NPC.velocity.Y < -4)
					NPC.velocity.Y += 0.08f;

				if (AITimer > 20)
					NPC.velocity.X *= 0.9f;
			}
			else if (AITimer < 40)
			{
				NPC.velocity.X = 0;
				NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -6, 0.3f);
				NPC.rotation = -MathHelper.PiOver2 * NPC.direction;
			}
			else if (AITimer == 40)
			{
				NPC.velocity.Y = 16;
				NPC.rotation = MathHelper.PiOver2 * NPC.direction;
				//
			}

			if (AITimer > 40 && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				//vfx and sfx and projs here

				_jumpState++; //use the variable to track the final ground pound too

				for (int i = -1; i <= 1; i++)
				{
					if (i == 0)
						continue;

					float distStep = Main.rand.NextFloat(16, 32) * i * 16;
					Vector2 projPosition = FindGroundFromPosition(NPC.Bottom + Vector2.UnitX * distStep) - Vector2.UnitY * 80;

					Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3, Main.myPlayer, Math.Abs(i) * 20);
				}

				if (_jumpState < maxBounces + maxPounds)
					NPC.velocity.Y = -16;
				else
					NPC.velocity.Y = -5;

				AITimer = 0;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));
			}

			NPC.velocity.Y += 0.38f;
		}

		else //rest before next attack
		{
			NPC.noGravity = false;
			NPC.knockBackResist = 0.3f;
			currentFrame.Y = 0;
			NPC.rotation = 0;
			AITimer++;

			if (AITimer > 120)
				ChangeState(SelectRandomState());
		}
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
				NPC.velocity = Vector2.Zero;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));

				//fx here
			}
		}

		if (_inGround)
		{
			AITimer++;

			NPC.velocity.X = (float)Math.Sin(AITimer * MathHelper.TwoPi / 120) * 5 + NPC.DirectionTo(Target.Center).X;
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

			if (AITimer % (undergroundTime / (numEruptions + 1)) == 0 && AITimer != undergroundTime)
			{
				//projectile here					
				Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center - Vector2.UnitY * 80, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3);

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 20));
			}

			if (AITimer > undergroundTime)
			{
				_inGround = false;
				NPC.velocity.Y = -15;
				AITimer = 0;
				_jumpState++;

				//fx here
				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));
			}
		}

		if (_jumpState == 2)
		{
			AITimer++;
			NPC.velocity.Y *= 0.95f;

			if (AITimer > restTime)
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
		if (AITimer++ == 0)
		{
			NPC.velocity.Y = -6;
			NPC.velocity.X /= 2;
			//fx here?

		}

		NPC.velocity *= 0.95f;
		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * 3 * AITimer / attackEndTime) / 10;

		if (AITimer == attackStartTime)
		{
			//proj here

			if (Main.netMode != NetmodeID.Server)
			{
				ParticleHandler.SpawnParticle(new TexturedPulseCircle(NPC.Center, Color.LightGoldenrodYellow, 1, 2400, 30, "GlowTrail", new Vector2(1, 1), EaseFunction.EaseCircularOut, true, 0.33f));
			}
		}

		if (AITimer > flashChargeStart && AITimer < flashExplostionTime)
		{
			//vfx here
		}

		if (AITimer == flashExplostionTime)
		{

			if (Main.netMode != NetmodeID.Server)
			{
				for (int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new DissipatingImage(NPC.Center, Color.Lerp(Color.LightGoldenrodYellow, Color.Goldenrod, 0.5f).Additive(), Main.rand.NextFloatDirection(), 0.66f, 0, "GodrayCircle", Vector2.Zero, new Vector2(3, 1.4f), 15));

			}
		}

		if (AITimer > attackEndTime)
			ChangeState(SelectRandomState());
	}
	#endregion
}