using Newtonsoft.Json.Linq;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	#region Cinematics

	#region Spawn Anim
	public float SpawnAnimation(ref bool retarget)
	{
		const int swarm_time = 120;
		const int roar_time = 120;

		/*Todo: 
		 * foreground scarab particles fly across the screen from bottom left to top right
		 * screenshake
		 * ground beneath player starts emitting particles
		 * scarab bursts out of ground and roars
		*/

		//Rumbling
		if (Counter <= swarm_time)
		{
			if (Counter == 0) //On-spawn effects
			{
				NPC.Center = (FindSandySurface(Target.Center.ToTileCoordinates(), out Point result) ? result.ToWorldCoordinates() : FindGroundFromPosition(Target.Center)) - new Vector2(0, NPC.height / 2);
				NPC.FaceTarget();

				if (!Main.dedServ)
				{
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY.RotatedByRandom(1f), 0.5f, 3, swarm_time * 2, uniqueIdentity: "ScarabeusSpawnShake"));

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
				Rectangle area = new((int)NPC.BottomLeft.X, (int)NPC.BottomLeft.Y, NPC.width, 2);
				for (int i = 0; i < Main.rand.Next(4); i++)
				{
					Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);
					Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Center));

					ParticleHandler.SpawnParticle(new SmokeCloud(Main.rand.NextVector2FromRectangle(area), particleVel, colors[0], Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
					{
						Pixellate = true,
						DissolveAmount = 1,
						Intensity = 0.9f,
						SecondaryColor = colors[1],
						TertiaryColor = colors[2],
						PixelDivisor = 3,
						Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
						ColorLerpExponent = 0.5f,
						Layer = ParticleLayer.BelowSolid
					});
				}

				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY.RotatedByRandom(1f), 3.5f, 3, swarm_time, uniqueIdentity: "ScarabeusSpawnShake"));

				FablesCameraFocus();

				if (Main.rand.NextBool(3))
					Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(area), DustID.Sand, new(0, -4), 0, default, Main.rand.NextFloat(0.7f, 1.2f));

				if (Counter % 20 == 0)
					BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX + NPC.velocity / 2);
			}
		}

		//Emerge
		else if (Counter <= swarm_time + roar_time) 
		{
			if (NPC.Opacity == 0) //One-time effects
			{
				NPC.noGravity = false;
				NPC.velocity.Y = -30;
				NPC.Opacity = 1;
				FablesIntroCard(roar_time);
				FablesToggleUI(false);
			}

			FablesCameraFocus();
			NPC.noTileCollide = NPC.velocity.Y < 0;

			if (Grounded) //Landed
			{
				NPC.FaceTarget();
				UpdateFrame(6, 12, PhaseOneProfile, false);
				NPC.rotation = 0;
				NPC.velocity.X = 0;
				ShiftUpToFloorLevel();

				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 2, 3, 20));
			}
			else
			{
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.DirectionTo(Target.Center - new Vector2(80 * NPC.direction, 0)).X * 10, 0.2f);
				NPC.rotation += 0.4f * NPC.direction;
				NPC.GravityMultiplier *= 3;
				NPC.MaxFallSpeedMultiplier *= 2f;

				SetFrame(RollFrame, PhaseOneProfile);
				showTrail = true;
			}
		}

		//End the cinematic
		else
		{
			NPC.dontTakeDamage = false;
			FablesToggleUI(true);

			//Bonus animation while wearing Scarabeus' mask, interrupted if the player hits it
			if (CanBeCharmed)
				ChangeState(AIState.Charmed);
			else
				ChangeState(FindAppropriateIdleState());
		}

		return 1f;

		static bool FindSandySurface(Point origin, out Point result)
		{
			const int range = 50;
			bool[] validSurfaceTiles = TileID.Sets.Factory.CreateBoolSet(TileID.Sand, ModContent.TileType<SavannaGrass>(), ModContent.TileType<SaltBlockDull>(), ModContent.TileType<SaltBlockReflective>());

			for (int x = origin.X - range / 2; x < origin.X + range / 2; x++)
			{
				int y = WorldMethods.FindGround(x, origin.Y);
				Tile tile = Main.tile[x, y];

				if (tile.HasTile && validSurfaceTiles[tile.TileType])
				{
					result = new Point(x, y);
					return true;
				}
			}

			result = Point.Zero;
			return false;
		}
	}

	public void FablesIntroCard(int cardDuration)
	{
		if (!CrossMod.Fables.Enabled || Main.LocalPlayer.DistanceSQ(NPC.Center) > 2000 * 2000)
			return;

		CrossMod.Fables.Instance.Call("vfx.displaybossintrocard", 
			"Scarabeus", 
			"Aura Monster", 
			cardDuration, 
			NPC.Center.X < Main.LocalPlayer.Center.X, 
			
			new Color(8, 0, 222) * 0.7f,  //Boss bar border color
			(new Color(225, 163, 39) * 0.8f) , //Title color
			new Color(10, 153, 245),    //Boss name chroma abberation color 1
			new Color(106, 81, 246),    //Boss name chroma abberation color 2

			"Crawling Complication 2",
			"Salvati");
	}

	public void FablesCameraFocus()
	{
		if (!CrossMod.Fables.Enabled)
			return;
		CrossMod.Fables.Instance.Call("vfx.cameraMagnetWithImmunity", NPC.Center, NPC.Center, 2000f, 1.4f);
	}

	public void FablesToggleUI(bool uiEnabled)
	{
		if (!CrossMod.Fables.Enabled)
			return;
		CrossMod.Fables.Instance.Call("vfx.toggleUIVisibility", uiEnabled);
	}
	#endregion

	#region Charmed easter egg
	public bool CanBeCharmed => NPC.life == NPC.lifeMax && Target.head == EquipLoader.GetEquipSlot(Mod, nameof(ScarabMask), EquipType.Head);

	public float CharmedIdle(ref bool retarget)
	{
		NPC.FaceTarget();
		SetFrame(0, 7, PhaseOneProfile);
		return 1f;
	}
	#endregion

	#region Phase transition
	public float TransitionAnimation(ref bool retarget)
	{
		NPC.velocity.X *= 0.5f;
		bool jumpingFrame = currentFrame == new Point(0, 2);
		NPC.noGravity = jumpingFrame;
		NPC.noTileCollide = jumpingFrame;
		NPC.dontTakeDamage = true;

		if (jumpingFrame)
		{
			NPC.dontTakeDamage = false;
			NPC.velocity.Y *= 0.95f;

			if (Counter >= 20)
			{
				ChangeState(AIState.Swarm);
				return 0f;
			}
		}
		else if (UpdateFrame(5, 12, PhaseTwoProfile, false) == FrameState.Stopped)
		{
			SetFrame(0, 2, PhaseTwoProfile);
			NPC.velocity.Y -= 15;
			Counter = 0;
		}
		else if (!Main.dedServ && Counter == 0) //Spawn effects
		{
			var easeAnimation = new AnimationSequence()
				.Add(new AnimationSequence.EaseSegment(30, Main.screenPosition, NPC.Center - Main.ScreenSize.ToVector2() / 2, EaseFunction.EaseCubicInOut))
				.Add(new AnimationSequence.WaitSegment((int)(60 / 12f * PhaseTwoProfile.GetFrameCount(5))))
				.Add(new SequenceCameraModifier.ReturnSegment(60, EaseFunction.EaseCubicInOut));

			Main.instance.CameraModifiers.Add(new SequenceCameraModifier(easeAnimation));
		}

		return 1f;
	}
	#endregion

	#endregion

	#region Idling between attacks
	public float IdleBetweenAttacks(ref bool retarget)
	{
		NPC.FaceTarget();

		//Pick a time to wait before the next attack
		if (Counter == 0 && Main.netMode != NetmodeID.MultiplayerClient)
		{
			ExtraMemory = Main.rand.NextFloat(1.8f, 2.2f) - 0.15f * DifficultyScale;
			NPC.netUpdate = true;
		}

		float nextAttackTime = ExtraMemory;

		if (!phaseTwo)
			GroundedIdle(ref nextAttackTime);
		else
			FlyHover(ref nextAttackTime);

		//Switch to an attack after enough time
		if (Counter > 1f)
		{
			ChangeState(SelectAttack());
			return 0f;
		}

		//We increment the idle counter
		return 1 / (60f * nextAttackTime);
	}

	#endregion

	#region Phase 1
	public void GroundedIdle(ref float nextAttackWaitTime)
	{
		NPC.rotation = 0f;

		//when grounded scarabeus doesn't actually collide with tiles like normal otherwise IT GETS STUCK EVERYWHERE LIKE A CHUD
		NPC.noTileCollide = true;
		NPC.noGravity = true;
		float playerDistanceX = Math.Abs(NPC.Center.X - Target.Center.X);

		//Get the shrunken down tile collision box for scarabeus
		Vector2 collisionOrigin = NPC.position;
		int collisionWidth = NPC.width;
		int collisionHeight = NPC.height;
		ShrinkTileHitbox(NPC, ref collisionOrigin, ref collisionWidth, ref collisionHeight);
		float collisionBottom = collisionOrigin.Y + collisionHeight;

		//Get some distance
		if (playerDistanceX < 110f && CurrentState == AIState.IdleTowardsPlayer)
			CurrentState = AIState.IdleAwayFromPlayer;
		//Draw near
		if (playerDistanceX > 190f && CurrentState == AIState.IdleAwayFromPlayer)
			CurrentState = AIState.IdleTowardsPlayer;
		//Fast back away if the player is really too close
		if (playerDistanceX < 60f)
			CurrentState = AIState.IdleBackAwayFast;
		//Slow down the fast back away if enough distance has been put between scarab and the player
		if (playerDistanceX > 100f && CurrentState == AIState.IdleBackAwayFast)
			CurrentState = AIState.IdleAwayFromPlayer;

		//Come towards the player if the sightline is broken
		if (playerDistanceX > 150 && !Collision.CanHitLine(new Vector2(NPC.Center.X + 40 * NPC.direction, collisionOrigin.Y - 16), 1, 1, Target.Top, 1, 1))
		{
			CurrentState = AIState.IdleTowardsPlayer;
			Enrage += 0.1f;
		}

		Rectangle targetHitbox = NPC.GetTargetData().Hitbox;
		bool abovePlayer = collisionBottom < targetHitbox.Bottom - 16;
		bool acceptTopSurfaces = !IgnorePlatforms; //Accept platforms if you aren't above the player's 
		bool insideSolids = Collision.SolidCollision(collisionOrigin, collisionWidth, collisionHeight, acceptTopSurfaces);
		bool upperBodyInSolids = Collision.SolidCollision(collisionOrigin, collisionWidth, collisionHeight - 4, acceptTopSurfaces);
		bool emptySpaceAhead = !Collision.SolidCollision(new Vector2(NPC.Center.X + collisionWidth / 2 * NPC.direction + Math.Min(16 * NPC.direction, 0), collisionOrigin.Y), 16, 80, acceptTopSurfaces);
		bool inWater = Collision.WetCollision(collisionOrigin, collisionWidth, collisionHeight + 8);
		bool wantsToSwim = inWater && NPC.Bottom.Y > targetHitbox.Bottom + 20;
		bool swimming = false;

		float walkSpeed = 0f;
		if (CurrentState == AIState.IdleTowardsPlayer)
			walkSpeed = 1.5f + Utils.GetLerpValue(100f, 500f, playerDistanceX, true) * 5f + Utils.GetLerpValue(3, 6f, Math.Abs(Target.velocity.X), true) * 3f + 0.5f * Enrage;
		else if (CurrentState == AIState.IdleAwayFromPlayer)
			walkSpeed = -2f;
		else if (CurrentState == AIState.IdleBackAwayFast)
		{
			walkSpeed = -5f;
			nextAttackWaitTime *= 0.8f; //Cooldown goes down faster while backing away fast
		}

		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, walkSpeed * NPC.direction, 0.05f);

		//Fall down fast if above player
		if (playerDistanceX < 40f && abovePlayer)
			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + NPC.gravity * 2f, 0.001f, 16f);

		//Stay at ground level if our feet are on the ground but not our upper body
		else if (insideSolids && !upperBodyInSolids)
			NPC.velocity.Y = 0f;

		//Fully colliding with tiles
		else if (insideSolids || wantsToSwim)
		{
			float riseSpeed = wantsToSwim ? 0.8f : 0.4f;
			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y - riseSpeed, -6f, !insideSolids ? 6f : 0f);
			nextAttackWaitTime *= 1.35f; //Attack less often if inside solids
			swimming = true;
		}

		//Jump if on the floor and there's a hole ahead
		else if (NPC.velocity.Y == 0f && emptySpaceAhead && CurrentState == AIState.IdleTowardsPlayer && playerDistanceX > 220)
			NPC.velocity.Y = -8f;

		//Fall down...
		else
			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + NPC.gravity * 1.5f, -8f, 16f);

		//NPC.Step();
		float fps = Math.Min(Math.Abs(NPC.velocity.X) * 5, 22) * NPC.direction;
		if (swimming && Math.Abs(fps) < 14f)
			fps = 14f * NPC.direction;

		UpdateFrame(1, (int)fps, PhaseOneProfile);
	}

	/*
	public float HornSwipe(ref bool retarget)
	{
		NPC.noGravity = false;
		NPC.velocity.X *= 0.5f;
		FrameState state = UpdateFrame(2, 12, PhaseOneProfile, false);

		if (currentFrame.Y is > 2 and < 7)
		{
			dealContactDamage = true;

			float distance = Target.Center.X - NPC.Center.X;
			if (Math.Sign(distance) == NPC.direction)
				NPC.velocity.X += distance * 0.1f;
		}

		if (state == FrameState.Stopped)
			ChangeState(SelectAttack());

		return 1f;
	}

	public void Skitter()
	{
		const int skitter_time = 40;

		NPC.noGravity = false;
		NPC.velocity.X = -NPC.direction * MathHelper.Lerp(12, 4, EaseFunction.EaseQuadOut.Ease(Counter / skitter_time));
		NPC.Step();

		UpdateFrame(1, (int)(NPC.direction * NPC.velocity.X) * 4, PhaseOneProfile);

		if (Counter > skitter_time)
			ChangeState(SelectAttack());
	}

	public void Leap()
	{
		ref float jumpState = ref NPC.ai[2];

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
						Vector2 desiredPos = Target.Center + Target.velocity * 20;

						if (NPC.Center.Y - Target.Center.Y > 80)
							NPC.velocity = NPC.GetArcVel(desiredPos - new Vector2(0, 30), NPC.gravity, Math.Clamp((NPC.Center.Y - (desiredPos.Y - 30)) / 16f, 15, 50), true);
						else
							NPC.velocity = NPC.GetArcVel(desiredPos, NPC.gravity, Math.Clamp(NPC.Center.Distance(desiredPos) / 36, 15, 30), true);

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
				else if (Target.Center.Y - NPC.Center.Y > 200) //Prompt a slam when too high up
				{
					jumpState = 3;
					Counter = 0;
				}

				break;

			case 2: //Recover
				NPC.noTileCollide = false;
				NPC.velocity.X *= 0.9f;

				if (UpdateFrame(6, 12, PhaseOneProfile, false) == FrameState.Stopped)
				{
					SetFrame(0, 0, PhaseOneProfile); //Return to the control frame
					ChangeState(SelectAttack());
				}

				break;

			case 3: //Optional ground slam
				SetFrame(RollFrame, PhaseOneProfile);
				NPC.rotation += Math.Min(Counter * 0.02f, 0.5f) * NPC.direction;
				NPC.noTileCollide = false;
				NPC.noGravity = true;

				if (Counter > 20)
				{
					NPC.velocity.X *= 0.98f;

					if (Grounded) //Land
					{
						if (!Main.dedServ)
						{
							Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 4, 2, 15, -1, "ScarabLanding"));
							Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
						}

						NPC.rotation = 0;
						ChangeState(SelectAttack());
					}
					else //Downward movement
					{
						showTrail = NPC.velocity.Y > 2;
						NPC.velocity.Y = Math.Min(NPC.velocity.Y + 1.2f, 16);
					}
				}
				else
				{
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.DirectionTo(Target.Center).X * 10, 0.1f);
					NPC.velocity.Y = NPC.velocity.Y * 0.93f;
				}

				break;
		}
	}
	*/

	public float RollAttack(ref bool retarget)
	{
		const int transition_time = 40;

		ref float dashState = ref NPC.ai[2];

		NPC.noTileCollide = false;
		NPC.noGravity = false;

		switch (dashState)
		{
			case 0: //Telegraph
				NPC.velocity.X *= 0.8f;
				NPC.FaceTarget();
				UpdateFrame(3, 10, PhaseOneProfile, false);

				if (Counter > 45)
				{
					Counter = 0;
					dashState++;
				}

				break;

			case 1: //Roll
				NPC.velocity.X = NPC.direction * 22;
				NPC.rotation += 0.3f * NPC.spriteDirection;
				NPC.Step();

				SetFrame(RollFrame, PhaseOneProfile);
				showTrail = true;
				dealContactDamage = true;
				//sfx here

				if ((Target.Center.X - NPC.Center.X) * NPC.direction < 30)
				{
					Counter = 0;
					dashState++;
				}

				if (NPC.collideX)
				{
					ChangeState(AIState.GroundPound); //bounce off of surfaces
					return 0f;
				}

				break;

			case 2: //Skid to a stop
				SetFrame(0, 6, PhaseOneProfile);
				NPC.rotation = 0;
				NPC.velocity.X *= 0.94f;

				if (Math.Sign(NPC.velocity.X) is int newDirection && newDirection != 0)
					NPC.direction = -newDirection;

				if (!Main.dedServ && Math.Abs(NPC.velocity.X) > 1 && Main.rand.NextBool())
				{
					Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Center));
					ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -Vector2.UnitY, colors[0], Main.rand.NextFloat(0.05f, 0.25f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
					{
						Pixellate = true,
						DissolveAmount = 1,
						SecondaryColor = colors[1],
						TertiaryColor = colors[2],
						PixelDivisor = 3,
						ColorLerpExponent = 0.5f
					});

					Dust.NewDust(NPC.BottomLeft, NPC.width, 16, DustID.Sand, 0, Main.rand.NextFloat(-4, -8), 0, default, Main.rand.NextFloat(0.5f, 0.9f));
				}

				if (Counter > transition_time)
				{
					Counter = 0;
					dashState++;
				}

				break;

			case 3: //End
				NPC.velocity.X /= 2;
				return GoBackToIdle();
		}

		return 1f;
	}

	#region Shockwave Slam
	public float ShockwaveAttack(ref bool retarget)
	{
		const int duration = 98;

		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.velocity.X *= 0.8f;

		if (Counter < 5)
			NPC.FaceTarget();

		int lastFrameY = currentFrame.Y;
		UpdateFrame(7, (int)(Profile.GetFrameCount(7) * 60f / duration), PhaseOneProfile);

		if (lastFrameY < 9 && currentFrame.Y >= 9)
		{
			dealContactDamage = true;
			//projectiles and sfx here

			if (Main.netMode != NetmodeID.MultiplayerClient)
				SpawnShockwaveFissure();
			if (!Main.dedServ)
			{
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 3, 35));
				Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
			}
		}

		if (Counter > duration)
			return GoBackToIdle();
		return 1f;
	}

	public void SpawnShockwaveFissure()
	{
		Vector2 fissurePos = FindGroundFromPositionIgnorePlatforms(NPC.Center);
		int delay = 5;
		float shockwaveHeight = 50f;

		for (int i = 0; i < 24; i++)
		{
			float spacing = 32f;
			float vfxVelocity = 1.2f;

			if (i < 15)
			{
				delay += 1;
				shockwaveHeight += 4f;
			}
			else if (i == 15)
			{
				delay += 17;
				shockwaveHeight += 50f;
			}
			else
			{
				spacing = 16f;
				vfxVelocity = 0.8f;
				shockwaveHeight += 5f;
			}

			Projectile.NewProjectile(NPC.GetSource_FromThis(), fissurePos, new Vector2(NPC.direction * vfxVelocity, 0f), ModContent.ProjectileType<SandShockwavePillar>(), NPC.damage / 4, 3, Main.myPlayer, delay, shockwaveHeight);

			Vector2 newFissurePos = FindGroundFromPositionIgnorePlatforms(fissurePos + new Vector2(spacing * NPC.direction, -40));
			if (Math.Abs(newFissurePos.Y - fissurePos.Y) > 200)
				break;

			fissurePos = newFissurePos;
		}
	}
	#endregion

	#region Ground pound
	public float GroundPoundAttack(ref bool retarget)
	{
		retarget = false;
		int max_bounces = phaseTwo ? 3 : 1;
		const int final_bounce_track_time = 40;
		const int air_pause_time = 16;
		const int rest_time = 90;
		const float downwardsSlamGravity = 0.38f;
		ref float bounceIndex = ref NPC.ai[2];
		float artificialGravityMultiplier = 1f;

		NPC.noTileCollide = true;
		NPC.noGravity = true;

		bool onTheFloor = OnTopOfTiles && NPC.velocity.Y >= 0;

		float targetDistanceX = Math.Abs(Target.Center.X -  NPC.Center.X);
		if (bounceIndex > 0 && targetDistanceX < 300)
		{
			float extraAllowedHeight = Utils.GetLerpValue(40f, 300f, targetDistanceX, true);
			onTheFloor &= NPC.Center.Y >= Target.Center.Y - 16 - extraAllowedHeight * 200;
		}

		if (bounceIndex < max_bounces)
		{
			SetFrame(RollFrame, PhaseOneProfile);
			dealContactDamage = true;

			//Bounce
			if (onTheFloor)
				Bounce(ref bounceIndex);
			else
				Spin();
		}

		else if (bounceIndex == max_bounces)
		{
			dealContactDamage = true;

			//Continue tracking in the air for a bit
			if (NPC.velocity.Y < 0)
			{
				Spin();
				if (Counter > final_bounce_track_time - 10)
					NPC.velocity.X *= 0.9f;

				//Slow down when about to overshoot target
				if (Math.Sign(NPC.velocity.X) != Math.Sign((Target.Center.X + Target.velocity.X * 20) - NPC.Center.X - NPC.direction * 100f))
					NPC.velocity.X *= 0.95f;
			}
			else 
			{
				//Start to unfurl as we fall down
				Point frame = NPC.velocity.Y > 6f ? new Point(7, 8) : NPC.velocity.Y > 1f ? new Point(0, 5) : RollFrame;
				SetFrame(frame, PhaseOneProfile); 				
				NPC.rotation = NPC.rotation.AngleLerpDirectional(NPC.velocity.Y * 0.003f * NPC.direction, 0.06f + Utils.GetLerpValue(0f, 6f, NPC.velocity.Y, true) * 0.14f, NPC.direction == -1);
			
				iridescenceBoost += 0.2f;
				if (Counter < final_bounce_track_time + air_pause_time)
				{
					artificialGravityMultiplier = 0f;
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -1.25f, 0.3f);
				}
				else
				{
					artificialGravityMultiplier = 1.3f;
					squishY = 1f + Utils.GetLerpValue(2f, 20f, NPC.velocity.Y, true) * 0.1f;
				}

				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(Target.Center.X - NPC.Center.X) * 4.6f, 0.01f);
			}

			//On tile collision
			if (onTheFloor)
			{
				bounceIndex++; //use the variable to track the final ground pound too
				ShiftUpToFloorLevel(5);
				NPC.velocity.Y = 0;
				squishY = 0.7f;
				artificialGravityMultiplier = 0f;

				if (!Main.dedServ)
				{
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 3, 35));
					Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
				}

				for (int i = -1; i <= 1; i += 2)
				{
					for (int j = 0; j < 4; j++)
					{
						float distStep = (200 + j * 56) * i ;
						Vector2 projPosition = FindGroundFromPositionIgnorePlatforms(NPC.Bottom + Vector2.UnitX * distStep);
						Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.UnitY * i * 0.5f, ModContent.ProjectileType<SandShockwavePillar>(), NPC.damage / 4, 3, Main.myPlayer, 1 + j * 3, 300 - j * 40f);
					}
				}
			}
		}

		else //rest before next attack
		{
			UpdateFrame(7, currentFrame.Y <= 11 ? 7 : 10, PhaseOneProfile, false);
			if (currentFrame.Y < 10)
				currentFrame.Y = 10;

			NPC.velocity *= 0.5f;
			NPC.rotation = 0;
			NPC.noGravity = false;
			NPC.noTileCollide = false;

			artificialGravityMultiplier = 0f;

			if (Counter > final_bounce_track_time + air_pause_time + rest_time)
				return GoBackToIdle();
		}

		NPC.velocity.Y += downwardsSlamGravity * artificialGravityMultiplier;
		return 1f;

		void Spin()
		{
			NPC.rotation += NPC.direction * 0.25f + NPC.velocity.X / 120;
		}

		void Bounce(ref float bounceIndex)
		{
			bounceIndex++;
			//Avoid scenarios where scarab ends up stuck in the floor
			ShiftUpToFloorLevel();

			NPC.TargetClosest();
			NPC.FaceTarget();
			Counter = 0;

			Vector2 bounceTarget = Target.Center + Target.velocity * 30f;
			float overshootMultiplier = Utils.GetLerpValue(1f, 3f, Target.velocity.X * NPC.direction, true) * 0.8f;
			float maxOvershootDistance = 400;
			float maxBounceXVel = 26f;

			if (bounceIndex == max_bounces)
			{
				overshootMultiplier = 2.5f;
				maxOvershootDistance = 600;
				maxBounceXVel = 36f;
			}

			bounceTarget.X += Math.Clamp(Target.Center.X - NPC.Center.X, -maxOvershootDistance, maxOvershootDistance) * overshootMultiplier;

			NPC.velocity = ArcVelocityHelper.GetArcVel(NPC.Center, bounceTarget, downwardsSlamGravity, minArcHeight: 300f, heightAboveTarget: 300f, maxXvel: maxBounceXVel);

			if (!Main.dedServ && bounceIndex > 1)
			{
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 4, 15, 1800));
				Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
			}
		}
	}
	#endregion

	public float DigAttack(ref bool retarget)
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
					NPC.noTileCollide = true;

					digState++;
				}
				else
				{
					NPC.velocity.X *= 0.95f;
				}

				break;

			case 1: //Fall into the ground
				SetFrame(0, 1, PhaseOneProfile);

				NPC.rotation = NPC.velocity.Y * 0.08f * NPC.direction;
				NPC.velocity.Y += 0.5f;
				NPC.GravityMultiplier *= 2;

				if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height - 4) || NPC.Opacity != 1)
				{
					if ((NPC.Opacity -= 0.1f) <= 0)
					{
						digState++; //Disappear into the ground
						Counter = 0;
					}
				}

				break;

			case 2: //Dig
				NPC.Opacity = 0;
				NPC.noGravity = true;
				NPC.velocity = Vector2.Zero;

				if (Counter < dig_time - 30)
				{
					NPC.Top = FindGroundFromPosition(Target.Center);
				}
				else
				{
					NPC.Top = FindGroundFromPosition(NPC.Top);
				}

				if (!Main.dedServ) //Digging visuals
				{
					for (int i = 0; i < Main.rand.Next(4); i++)
					{
						Vector2 particlePos = NPC.Center - Vector2.UnitY * 48;
						particlePos += Main.rand.NextFloat(-64, 64) * Vector2.UnitX;

						Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);
						Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Center));

						ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, colors[0], Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
						{
							Pixellate = true,
							DissolveAmount = 1,
							Intensity = 0.9f,
							SecondaryColor = colors[1],
							TertiaryColor = colors[2],
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

					if (NPC.DistanceSQ(Target.Center) < 50 * 50)
						NPC.velocity.Y -= 10;
					else
						NPC.velocity = NPC.GetArcVel(Target.Center + Vector2.Normalize(Target.velocity) * 400, NPC.gravity, 15, true);

					NPC.noGravity = false;
					NPC.FaceTarget();
				}

				break;

			case 3: //Emerge and land
				if (!Grounded)
					SetFrame(0, 1, PhaseOneProfile);

				NPC.rotation = NPC.velocity.ToRotation() + ((NPC.direction == -1) ? MathHelper.Pi : 0);
				NPC.Opacity = Math.Min(NPC.Opacity + 0.1f, 1);
				NPC.GravityMultiplier *= 2;

				if (Counter > 10 && NPC.velocity.Y >= 0)
				{
					NPC.noTileCollide = false;

					if (Grounded) //Land
					{
						NPC.velocity.X *= 0.1f;
						NPC.rotation = 0;
						NPC.behindTiles = false;

						if (UpdateFrame(6, 12, PhaseOneProfile, false) == FrameState.Stopped)
						{
							SetFrame(0, 0, PhaseOneProfile); //Change back to the control frame to prevent jitters
							return GoBackToIdle();
						}

						return 1f;
					}
				}

				break;
		}

		return 1f;
	}
	#endregion

	#region Phase 2
	public void FlyHover(ref float nextAttackWaitTime)
	{
		//Attacks faster in P2 just by default
		nextAttackWaitTime *= 0.6f;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
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

		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * Counter) / 10;

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
	}

	public float FlyingDashAttack(ref bool retarget)
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

			SetFrame(0, 0, PhaseTwoProfile);
			showTrail = true;

			NPC.velocity = Vector2.UnitX.RotatedBy(dashRotation) * 18;
			NPC.direction = Math.Sign(NPC.velocity.X);
			NPC.rotation = NPC.velocity.ToRotation() + ((NPC.direction == -1) ? MathHelper.Pi : 0);

			if (Counter > idle_time + dash_time)
			{
				NPC.velocity *= 0.4f;
				return GoBackToIdle();
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
			return GoBackToIdle();
		}

		return 1f;
	}

	/*
	public void ChainGroundPound()
	{
		const int expire_time = 300;
		const int idle_time = 90;

		ref float jumpState = ref NPC.ai[2];

		switch ((int)jumpState)
		{
			case 0: //Track Target and prepare for a ground pound
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

				break;

			case 1: //Ground pound
				if (Profile == PhaseOneProfile)
					SetFrame(RollFrame, PhaseOneProfile);
				else if (UpdateFrame(3, 12, PhaseTwoProfile, false) == FrameState.Stopped)
					Profile = PhaseOneProfile;

				if (Grounded) //Collide
				{
					if (!Main.dedServ)
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 5, 9, 15));

					Counter = 0;
					Profile = PhaseOneProfile;
					NPC.FaceTarget();

					if ((jumpState += 0.35f) < 2)
					{
						NPC.velocity.Y = -18; //Bounce up
					}
					else
					{
						SetFrame(4, 3, PhaseTwoProfile);
						NPC.rotation = 0;
					}
				}

				dealContactDamage = true;
				NPC.noTileCollide = false;
				NPC.velocity.Y += 0.6f;
				NPC.velocity.X = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(Target.Center) * 6, 0.1f).X; //Track Target

				if (currentFrame == RollFrame)
					NPC.rotation += 0.1f * NPC.direction;

				break;

			case 2: //Stationary slam
				NPC.velocity.X *= 0.9f;
				FrameState state = UpdateFrame(4, 12, PhaseTwoProfile, false);

				if (currentFrame.Y is > 2 and < 7)
					dealContactDamage = true;

				if ((jumpState > 2.5f) ? state == FrameState.Stopped : currentFrame.Y == 9)
				{
					SetFrame(4, 0, PhaseTwoProfile);

					if ((jumpState += 0.35f) > 3)
					{
						SetFrame(3, PhaseTwoProfile.GetFrameCount(3) - 1, PhaseTwoProfile);
						NPC.velocity.Y -= 8;
					}
				}

				break;

			case 3: //Transition hop
				NPC.velocity.X *= 0.9f;

				if (UpdateFrame(3, -12, PhaseTwoProfile, false) == FrameState.Stopped)
					ChangeState(FlyHover);

				break;
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
		NPC.behindTiles = true;

		if (jumpState == 0) //Jump up
		{
			jumpState++;
			NPC.velocity = new Vector2(6 * NPC.direction, -12);
		}

		if (groundState == 0 && jumpState == 1) //Fall into the ground
		{
			if (Profile != PhaseOneProfile && UpdateFrame(3, 12, PhaseTwoProfile, false) == FrameState.Stopped)
				SetFrame(RollFrame, PhaseOneProfile);

			NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.4f, 24);

			if (currentFrame == RollFrame)
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
				Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Center));
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Center - Vector2.UnitY * 32, -Vector2.UnitY * 8, colors[0], Main.rand.NextFloat(0.1f, 0.25f), EaseFunction.EaseCubicOut, 30)
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = colors[1],
					TertiaryColor = colors[2],
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
				ChangeState(SelectAttack());
			}
		}
	}
	*/

	public float SwarmAttack(ref bool retarget)
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
			return GoBackToIdle();
		return 1f;
	}
	#endregion
}