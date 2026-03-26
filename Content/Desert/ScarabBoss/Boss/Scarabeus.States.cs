using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Desert.ScarabBoss.Dusts;
using SpiritReforged.Content.Desert.ScarabBoss.Gores;
using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using static SpiritReforged.Common.Misc.AnimationSequence;
using static SpiritReforged.Content.Desert.ScarabBoss.Boss.Scarabeus;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	#region Cinematics

	#region Spawn Anim
	public float SpawnAnimation(ref bool retarget)
	{
		const int swarm_time = 120;
		const int roar_time = 80;
		const int pause_roar_time = 60;

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
						int spawnDelayRange = swarm_time;
						int spawnDelayStatic = backgroundScarab ? 0 : (int)(swarm_time * 0.66f);
						scarabPos += new Vector2(-Main.rand.NextFloat(1300, 1400), Main.rand.NextFloat(200, 500));
						if (!backgroundScarab)
						{
							scarabPos.X = Target.Center.X + Main.rand.NextFloat(1600, 2000);
							scarabPos.Y -= 200;
						}

						ParticleHandler.SpawnQueuedParticle(new ScarabParticle(scarabPos, Main.rand.NextFloat(0.3f, 0.9f), backgroundScarab ? 1 : -1, backgroundScarab), Main.rand.Next(spawnDelayRange) + spawnDelayStatic);
					}

					Vector2 targetPosition = NPC.Center - Main.ScreenSize.ToVector2() / 2;
					var easeAnimation = new AnimationSequence()
						.Add(new EaseSegment(120, Main.screenPosition, targetPosition, EaseFunction.EaseCubicInOut))
						.Add(new FollowSegment(270, NPC))
						.Add(new SequenceCameraModifier.ReturnSegment(60, EaseFunction.EaseCubicInOut));

					Main.instance.CameraModifiers.Add(new SequenceCameraModifier(easeAnimation));
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

				//FablesCameraFocus();

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
				if (!Main.dedServ)
				{
					for (int i = 0; i < 20; i++)
					{
						Vector2 pos = NPC.Bottom;

						pos.X += Main.rand.Next(-30, 30);

						KickupDust(pos, new Vector2(1f * NPC.direction, -2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5), ParticleLayer.BelowSolid);
						
						KickupDust(pos, new Vector2(0.5f * NPC.direction, -1f).RotatedByRandom(1.5f) * Main.rand.NextFloat(1, 3));
					}

					GroundImpactVFX(2f);
				}

				SoundEngine.PlaySound(ChitterSound, NPC.Center);
				SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);

				NPC.noGravity = false;
				NPC.velocity.Y = -30;
				NPC.Opacity = 1;
			}

			//FablesCameraFocus();
			NPC.noTileCollide = NPC.velocity.Y < 0;

			if (OnTopOfTiles) //Landed
			{
				if (ExtraMemory < 2)
				{
					if (!Main.dedServ)
					{
						for (int i = 0; i < 15; i++)
						{
							Vector2 pos = NPC.Bottom;

							pos.X += Main.rand.Next(-30, 30);

							KickupDust(pos, new Vector2(1f * NPC.direction, -2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5), ParticleLayer.BelowSolid);

							KickupDust(pos, new Vector2(0.5f * NPC.direction, -1f).RotatedByRandom(1.5f) * Main.rand.NextFloat(1, 3));
						}

						GroundImpactVFX(1.5f);
					}

					SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);

					ExtraMemory++;
				}

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
				//NPC.rotation += NPC.velocity.X * 0.04f;
				NPC.rotation += NPC.direction * 0.4f;
				NPC.GravityMultiplier *= 3;
				NPC.MaxFallSpeedMultiplier *= 2f;

				SetFrame(RollFrame, PhaseOneProfile);
				trailOpacity = 0.4f;
			}
		}

		//End the cinematic
		else if (Counter >= swarm_time + roar_time + pause_roar_time)
		{
			SetFrame(7, 0, PhaseOneProfile);

			Counter = 0;
			ExtraMemory = 0;

			//Bonus animation while wearing Scarabeus' mask, interrupted if the player hits it
			if (CanBeCharmed)
            {
                NPC.dontTakeDamage = false;
                ChangeState(AIState.Charmed);
            }
            else if (CanDance)
				ChangeState(AIState.Dance);
			else
				ChangeState(AIState.Roar);
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

		if (!CanBeCharmed)
		{
			ChangeState(AIState.Roar);
			Counter = 0;
			return 0f;
		}

		return 1f;
	}
	#endregion

	#region SpawnRoar

	public float Roar(ref bool retarget)
	{
		int lastFrameY = currentFrame.Y;
		int framerate = 10;
		
		FrameState updateResult = UpdateFrame(7, framerate, PhaseOneProfile, false);

		if (Counter % 10 == 0 && lastFrameY is <= 7 and >= 3)
			ParticleHandler.SpawnParticle(new RoarRing(NPC.Center, 0.35f, 4500, 30, EaseFunction.EaseCubicIn, false, 0.35f));

		Music = Phase1Music;
		Main.musicFade[Main.curMusic] = 1f;

		if (lastFrameY == 2 && ExtraMemory < 1)
		{
			FablesIntroCard(100);
			FablesToggleUI(false);

			if (!Main.dedServ)
			{
				for (int i = 0; i < 5; i++)
				{
					Vector2 pos = NPC.BottomRight;
					if (NPC.direction == -1)
						pos = NPC.BottomLeft;

					KickupDust(pos, new Vector2(0.2f * NPC.direction, -2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5), ParticleLayer.BelowSolid);

					KickupDust(pos, new Vector2(0.5f * NPC.direction, -1f).RotatedByRandom(1.5f) * Main.rand.NextFloat(1, 3));
				}
			}

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);

			ExtraMemory++;
		}

		if (lastFrameY == 4 && ExtraMemory < 2)
		{
			if (!Main.dedServ)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitX * NPC.direction, 10, 10, 60));

			//SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.1f}, NPC.Center);
			SoundEngine.PlaySound(ChitterSound, NPC.Center);

			ExtraMemory++;
		}

		int roarTime = 15;
		if (CrossMod.Fables.Enabled)
			roarTime = 45;

		if (lastFrameY == 7 && ExtraMemory < roarTime)
		{
			ExtraMemory++;
			SetFrame(7, 7, PhaseOneProfile);
		}

		if (lastFrameY == 9 && ExtraMemory < roarTime + 1)
		{
			if (!Main.dedServ)
			{
				for (int i = 0; i < 20; i++)
				{
					Vector2 pos = NPC.BottomRight;
					if (NPC.direction == -1)
						pos = NPC.BottomLeft;

					KickupDust(pos, new Vector2(1f * NPC.direction, -2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5), ParticleLayer.BelowSolid);

					KickupDust(pos, new Vector2(0.5f * NPC.direction, -1f).RotatedByRandom(1.5f) * Main.rand.NextFloat(1, 3));
				}
			}

			FablesToggleUI(true);

			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, -Vector2.UnitY, 10, 10, 60));

			BouncingTileWave(7, 8f, 40);

			SoundEngine.PlaySound(ChitterSound, NPC.Center);
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);

			ExtraMemory++;
		}

		NPC.FaceTarget();
		
		if (updateResult == FrameState.Stopped)
		{
			ChangeState(FindAppropriateIdleState());

			Counter = 0;
			return 0f;
		}

		return 1f;
	}

	#endregion

	#region Dancing easter egg
	public bool CanDance => Main.newMusic == ScarabRadio.MusicSlot || Target.HeldItem.type == ModContent.ItemType<ScarabRadio>();

	public float DanceIdle(ref bool retarget)
	{
		NPC.FaceTarget();
		UpdateFrame(4, 10, PhaseOneProfile);

		if (!CanDance)
		{
			ChangeState(AIState.IdleBackAwayFast);
			Counter = 0;
			return 0f;
		}

		return 1f;
	}
	#endregion

	#region Phase transition
	public float TransitionAnimation(ref bool retarget)
	{
		NPC.velocity.X *= 0.5f;
		bool jumped = currentFrame == new Point(0, 2) && Profile == PhaseTwoProfile;
		NPC.noGravity = jumped;
		NPC.noTileCollide = jumped;
		NPC.dontTakeDamage = true;

		//Slower framerate right before the wings reveal
		int framerate = currentFrame.Y == 6 ? 5 : 12;

		Main.musicFade[Main.curMusic] = 1;

		//Spawn effects
		if (!jumped && Counter == 0) 
		{
			ShiftUpToFloorLevel();

			if (!Main.dedServ)
			{
				var easeAnimation = new AnimationSequence()
					.Add(new AnimationSequence.EaseSegment(30, Main.screenPosition, NPC.Center - Main.ScreenSize.ToVector2() / 2, EaseFunction.EaseCubicInOut))
					.Add(new AnimationSequence.WaitSegment((int)(60 / 12f * PhaseTwoProfile.GetFrameCount(5))))
					.Add(new SequenceCameraModifier.ReturnSegment(60, EaseFunction.EaseCubicInOut));
				Main.instance.CameraModifiers.Add(new SequenceCameraModifier(easeAnimation));
			}
		}

		if (!jumped)
		{
			if (NPC.velocity.Y > 0 && OnTopOfTiles)
				NPC.velocity.Y = 0;

			if (UpdateFrame(5, framerate, PhaseTwoProfile, false) == FrameState.Stopped)
			{
				if (!Main.dedServ)
				{
					for (int i = 0; i < 20; i++)
					{
						Vector2 pos = NPC.Bottom;

						pos.X += Main.rand.Next(-30, 30);

						KickupDust(pos, new Vector2(0, -2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5), ParticleLayer.BelowSolid);

						KickupDust(pos, new Vector2(0, -1f).RotatedByRandom(1.5f) * Main.rand.NextFloat(1, 3));
					}
				}

				SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);

				SetFrame(0, 2, PhaseTwoProfile);
				NPC.velocity.Y -= 15;
				Counter = 0;
				NPC.noTileCollide = true;
				NPC.noGravity = true;
			}
		}
		else if (jumped)
		{
			NPC.dontTakeDamage = false;
			NPC.velocity.Y *= 0.95f;

			if (Counter >= 20)
			{
				ChangeState(AIState.Swarm);
				return 0f;
			}
		}

		return 1f;
	}
	#endregion
	#region Death Animation
	public float DeathAnimation(ref bool retarget)
	{
		int frameCounter = (int)MathHelper.Lerp(30, 0, Math.Min(Counter / 300f, 1f));

		if (OnTopOfTiles)
			frameCounter = 0;

		UpdateFrame(0, frameCounter, SimulatedProfile);
		wingFrameCounter += 25f / 60f * (frameCounter / 30f);

		if (Counter == 0)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/KillStart") with { Volume = 0.2f});

			NPC.hide = true;

			ExtraMemory = 0;
			NPC.velocity = Vector2.UnitY * -2f + NPC.DirectionTo(Target.Center) * -4f;

			if (CurrentState != AIState.IdleTowardsPlayer && Math.Abs(NPC.velocity.Y) < 5)
				NPC.velocity.Y -= 4f;

			NPC.noGravity = true;

			if (!Main.dedServ)
			{
				Vector2 targetPosition = NPC.Center - Main.ScreenSize.ToVector2() / 2;
				var easeAnimation = new AnimationSequence()
					.Add(new EaseSegment(40, Main.screenPosition, targetPosition, EaseFunction.EaseCubicInOut))
					.Add(new FollowSegment(160, NPC))
					.Add(new SequenceCameraModifier.ReturnSegment(60, EaseFunction.EaseCubicInOut));

				Main.instance.CameraModifiers.Add(new SequenceCameraModifier(easeAnimation));
			}
		}

		if (Counter % 20 == 0)
		{
			if (!Main.dedServ)
			{
				Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2);

				Vector2 velocity = Main.rand.NextVector2CircularEdge(5f, 5f);

				for (int i = 0; i < 7; i++)
				{
					pos += Main.rand.NextVector2Circular(5f, 5f);

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity.RotatedByRandom(0.65f) * Main.rand.NextFloat(), Color.DarkOrange, Color.Yellow * 0.3f, 0.1f, EaseBuilder.EaseCircularIn, 50, false)
					{
						Pixellate = true,
						DissolveAmount = 1,
						Intensity = 0.9f,
						PixelDivisor = 3,
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, -NPC.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(), Color.DarkOrange, Color.Yellow * 0.3f, 0.13f, EaseBuilder.EaseCircularIn, 50, false)
					{
						Pixellate = true,
						DissolveAmount = 1,
						Intensity = 0.9f,
						PixelDivisor = 3,
					});

					Dust.NewDustPerfect(pos, ModContent.DustType<ScarabeusBlood>(), velocity.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.8f), 50 + Main.rand.Next(100), default, 1.3f);

					Dust.NewDustPerfect(pos, ModContent.DustType<ScarabeusBlood>(), velocity.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.8f), 50 + Main.rand.Next(100), default, 1.6f).noGravity = true;
				}

				if (Main.rand.NextBool(3))
				{
					var gore = Gore.NewGoreDirect(NPC.GetSource_Death(), pos, velocity, ModContent.GoreType<ScarabeusGuts>());
					gore.position -= new Vector2(gore.Width, gore.Height) / 2;
				}			
			}

			SoundEngine.PlaySound(NPC.HitSound, NPC.Center);

			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2CircularEdge(1f, 1f), 5, 3, 25));
			_shakeTimer = 20;

			NPC.velocity.Y += 0.2f;
			NPC.position.Y += 16;
		}

		if (!OnTopOfTiles && ExtraMemory <= 0)
		{
			NPC.velocity.Y += 0.05f;

			if (NPC.velocity.Y > 4 && NPC.velocity.Y < 16)
				NPC.velocity.Y *= 1.1f;

			if (NPC.velocity.Y > 16)
				NPC.velocity.Y = 16f;

			if (Counter < 150)
				NPC.velocity.X += 0.3f * (float)Math.Cos(Counter / 20f);

			NPC.velocity.X *= MathHelper.Lerp(0.99f, 0.975f, Math.Min(Counter / 150f, 1f));

			NPC.direction = Math.Sign(NPC.velocity.X);

			NPC.rotation = NPC.velocity.X * (NPC.velocity.Y / 16f) * 0.3f;
		}
		else if (NPC.velocity.Y > 0)
		{
			NPC.velocity *= 0.15f;

			if (!Main.dedServ)
			{
				for (int i = 0; i < 55; i++)
				{
					Vector2 pos = NPC.BottomRight;
					if (NPC.direction == -1)
						pos = NPC.BottomLeft;

					pos.X += Main.rand.NextFloat(-60, 60);

					KickupDust(pos, -NPC.velocity.RotatedByRandom(1.5f) * Main.rand.NextFloat(1.5f), ParticleLayer.AboveSolid);

					KickupDust(pos, -NPC.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(2f), ParticleLayer.BelowSolid);
				}

				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 5, 3, 20));
			}

			SoundEngine.PlaySound(SmallChitterSound, NPC.Center);
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);
			
			//SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/KillEnd") with { Volume = 0.35f });

			NPC.life = 0;
			NPC.checkDead();
			NPC.HitEffect();
			NPC.active = false;
		}

		return 1f;
	}
	#endregion
	#endregion

	#region Idling between attacks
	public float IdleBetweenAttacks(ref bool retarget)
	{
		if (Main.rand.NextBool(300))
		{
			SoundStyle chitter = SmallChitterSound;

			if (Main.rand.NextBool(3))
				chitter = ChitterSound;

			SoundEngine.PlaySound(chitter, NPC.Center);
		}	

		NPC.FaceTarget();
		NPC.dontTakeDamage = false;

		//Pick a time to wait before the next attack
		if (Counter == 0 && Main.netMode != NetmodeID.MultiplayerClient)
		{
			ExtraMemory = Main.rand.NextFloat(STAT_MIN_IDLE_TIME, STAT_MAX_IDLE_TIME);
			if (Main.masterMode)
				ExtraMemory -= STAT_IDLE_TIME_REDUCTION_MASTER;
			else
				ExtraMemory -= STAT_IDLE_TIME_REDUCTION_EXPERT;

			if (LastAttack == AIState.Swarm)
				ExtraMemory *= 1.65f;
			if (!phaseTwo)
				ExtraMemory *= STAT_IDLE_TIME_HEALTH_PERCENT_MIN_MULTIPLIER_P1 + (1 - STAT_IDLE_TIME_HEALTH_PERCENT_MIN_MULTIPLIER_P1) * Utils.GetLerpValue(PHASE_2_HEALTH_THRESHOLD, 1f, NPC.life / (float)NPC.lifeMax, true);
			else
				ExtraMemory *= STAT_IDLE_TIME_HEALTH_PERCENT_MIN_MULTIPLIER_P2 + (1 - STAT_IDLE_TIME_HEALTH_PERCENT_MIN_MULTIPLIER_P2) * Utils.GetLerpValue(0f, PHASE_2_HEALTH_THRESHOLD, NPC.life / (float)NPC.lifeMax, true);

			ExtraMemory = MathHelper.Max(0.01f, ExtraMemory);
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

	public void GroundedIdle(ref float nextAttackWaitTime)
	{
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
		if (playerDistanceX > 130f && CurrentState == AIState.IdleBackAwayFast)
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
		bool falling = false;

		float walkSpeed = 0f;
		if (CurrentState == AIState.IdleTowardsPlayer)
			walkSpeed = 1.5f + Utils.GetLerpValue(100f, 500f, playerDistanceX, true) * 5f + Utils.GetLerpValue(3, 6f, Math.Abs(Target.velocity.X), true) * 3f + 0.5f * Enrage;
		else if (CurrentState == AIState.IdleAwayFromPlayer)
			walkSpeed = -2f;
		else if (CurrentState == AIState.IdleBackAwayFast)
		{
			walkSpeed = -6.5f;
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
		{
			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + NPC.gravity * 1.5f, -8f, 16f);
			//Heavily slowdown wait time while scarab is falling
			nextAttackWaitTime *= 12f;
			falling = NPC.velocity.Y > 5;
		}

		//NPC.Step();
		float fps = Math.Min(Math.Abs(NPC.velocity.X) * 5, 22) * (Math.Sign(NPC.velocity.X) * NPC.direction);
		if (swimming && Math.Abs(fps) < 14f)
			fps = 14f * NPC.direction;

		if (falling)
		{
			SetFrame(0, 8, PhaseOneProfile);
			NPC.rotation = NPC.velocity.X * 0.03f; 
		}
		else
		{

			/*if (Main.rand.NextBool(15))
			{
				Vector2 pos = NPC.BottomLeft;
				if (NPC.direction == -1)
					pos = NPC.BottomRight;

				KickupDust(pos, new Vector2(-1f * NPC.direction, -1f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 3));
			}*/

			NPC.rotation = 0f;
			UpdateFrame(1, (int)fps, PhaseOneProfile);
		}
	}
	
	public void FlyHover(ref float nextAttackWaitTime, float wingbeatSpeed = 1f)
	{
		//Attacks slower in P2 because its flying so its harder to hit
		nextAttackWaitTime *= STAT_IDLE_TIME_P2_MULTIPLIER;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.rotation = NPC.velocity.X * 0.05f;
		NPC.FaceTarget();
		wingFrameCounter += 22f / 60f; //30 fps for wings specifically

		UpdateFrame(0, (int)(15 * wingbeatSpeed), SimulatedProfile); //UpdateFrame(2, (int)(15 * wingbeatSpeed), PhaseTwoProfile);

		float heightAboveGround = FindGroundFromPositionIgnorePlatforms(NPC.Center).Y - NPC.Center.Y;

		//Vertical movement
		if (heightAboveGround < 128)
		{
			NPC.velocity.Y -= 0.1f;
			if (heightAboveGround < 0)
				 NPC.velocity.Y -= 0.15f;
		}

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
	#endregion

	//Phase 1
	#region Roll
	public float RollAttack(ref bool retarget)
	{
		retarget = false;
		const int transition_time = 40;
		ref float dashState = ref NPC.ai[2];
		float rollSpeed = phaseTwo ? 30 : 22;

		NPC.behindTiles = dashState >= 1 && dashState < 3;

		if (Counter == 0 && dashState == 0 && !phaseTwo)
			NPC.FaceTarget();

		switch (dashState)
		{
			//Anticipation
			case 0:
				bool doRollBounceTelegraph = false;

				if (!phaseTwo)
				{
					NPC.velocity.X *= 0.8f;
					//Lil bob before the jump
					if (UpdateFrame(4, 12, PhaseOneProfile, false) != FrameState.Stopped)
					{
						NPC.velocity.X *= 0.95f;
						NPC.noGravity = false;
						NPC.noTileCollide = false;
					}
					else
						doRollBounceTelegraph = true;
				}
				//In phase 2, scarabeus can only do the roll dash from its flying swoop dash, and it starts with its velocity conserved
				else
				{
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.03f);
					NPC.velocity.Y += 0.15f;

					SetFrame(RollFrame, PhaseTwoProfile);
					GroundPoundSpin();
					dealContactDamage = true;
					NPC.noGravity = false;

					//Hitting the ground
					if (NPC.velocity.Y > 0 && OnTopOfTiles)
					{
						NPC.velocity.Y *= 0;
						doRollBounceTelegraph = true;
						ShiftUpToFloorLevel();

						SoundEngine.PlaySound(BounceSound, NPC.Center);
						GroundImpactVFX(Math.Abs(NPC.velocity.Y));
					}
				}

				if (doRollBounceTelegraph)
				{
					if (!phaseTwo)
						NPC.position.Y -= 20;

					NPC.velocity.Y -= 9f;
					NPC.velocity.X = 0f;
					NPC.velocity.X += NPC.DirectionTo(Target.Center).X * 0.2f;
					NPC.noTileCollide = true;
					NPC.noGravity = false;
					NPC.direction = (NPC.Center.X - Target.Center.X) < 0 ? 1 : -1;
					SetFrame(RollFrame, phaseTwo ? PhaseTwoProfile : PhaseOneProfile);
					Counter = 0;
					dashState++;

					if (!Main.dedServ)
					{
						for (int i = 0; i < 10; i++)
						{
							Vector2 pos = NPC.BottomLeft;
							if (NPC.direction == -1)
								pos = NPC.BottomRight;

							KickupDust(pos, new Vector2(-1f * NPC.direction, -1f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5));
						}
					}

					SoundEngine.PlaySound(SmallChitterSound, NPC.Center);
					SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);

					if (!Main.dedServ)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 4, 5, 35));
						Collision.HitTiles(NPC.TopLeft, NPC.velocity, NPC.width, NPC.height + 14);
						ScarabHeatHazeShaderData.HeatHazeIntensity = 0.1f;
					}
				}

				break;

			case 1: // Bounce before the roll
				SetFrame(RollFrame, phaseTwo ? PhaseTwoProfile : PhaseOneProfile);
				GroundPoundSpin(true);
				//NPC.rotation += 0.02f * NPC.direction * Math.Max(0, NPC.velocity.Y);

				//Fall faster in P2 for faster telegraph
				if (phaseTwo)
					NPC.velocity.Y += phaseTwo ? 0.12f : 0.08f;

				dealContactDamage = true;
				NPC.noGravity = false;

				//Hitting the ground
				if (NPC.velocity.Y > 0 && OnTopOfTiles)
				{
					NPC.direction = (NPC.Center.X - Target.Center.X) < 0 ? 1 : -1;
					Counter = 0;
					dashState++;
					NPC.velocity.Y = 0;
					NPC.velocity.X = NPC.direction * 12f;
					NPC.noGravity = true;
					NPC.noTileCollide = true;

					if (!Main.dedServ)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 5, 35));

						SoundEngine.PlaySound(RollStartSound, NPC.Center, RollSoundTracking);
						SoundEngine.PlaySound(BounceSound with { Volume = 0.4f}, NPC.Center);
						GroundImpactVFX(1.5f);
					}
				}

				break;

			case 2: //Roll
				NPC.noGravity = true;
				NPC.noTileCollide = true;

				float interpolant = 0f;

				float dist = Math.Abs(Target.Center.X - NPC.Center.X);
				if (dist > 300f)
					interpolant = 1f;
				else if (dist > 100f)
					interpolant = (dist - 100f) / 200f;
	
				float adjustedRollSpeed = rollSpeed * MathHelper.Lerp(1f, 1.5f, interpolant);

				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * adjustedRollSpeed, 0.1f);
				NPC.rotation += 0.01f * NPC.velocity.X;

				float floorHeight = FindGroundFromPositionIgnorePlatforms(NPC.Center, Math.Max(NPC.Center.Y - 14f, Target.Bottom.Y)).Y;

				//Match floor height when the slope is low enough
				if (floorHeight > NPC.Top.Y && floorHeight < NPC.Bottom.Y + 40)
				{
					NPC.velocity.Y = 0;
					NPC.position.Y = MathHelper.Lerp(NPC.position.Y, floorHeight - NPC.height, 0.6f);
				}
				//Fall if theres no floor
				else if (!OnTopOfTiles && floorHeight > NPC.Bottom.Y)
					NPC.velocity.Y += 0.45f;
				//Bonk and transition to ground pound
				else if (floorHeight < NPC.Top.Y)
				{
					//BONK effects
					if (!Main.dedServ)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 5, 35));
						Collision.HitTiles(NPC.TopLeft, NPC.velocity, NPC.width, NPC.height + 14);
						ScarabHeatHazeShaderData.HeatHazeIntensity = 0.7f;
					}

					ChangeState(AIState.GroundPound); //bounce off of surfaces
					NPC.velocity.X = 0;
					NPC.velocity.Y = 0;
					return 0f;
				}

				SetFrame(RollFrame, phaseTwo ? PhaseTwoProfile : PhaseOneProfile);
				trailOpacity = Math.Min(1, Counter / 25f);
				dealContactDamage = true;
				if (!Main.dedServ)
					CreateRollParticles();
				//sfx here

				if ((Target.Center.X - NPC.Center.X) * NPC.direction < -100)
				{
					//in phase 2 we just don't do the skid
					if (phaseTwo)
					{
						NPC.velocity.X *= 0.5f;
						NPC.rotation = 0;
						SetFrame(new Point(0, 3), PhaseTwoProfile);
						trailOpacity = 0;
						return GoBackToIdle();
					}

					Counter = 0;
					dashState++;
					NPC.direction = NPC.velocity.X < 0 ? 1 : -1;
					trailOpacity = 0f;
					NPC.rotation = 0;
					SetFrame(0, 6, PhaseOneProfile);

					if (!Main.dedServ)
						SoundEngine.PlaySound(SlideScreechSound, NPC.Center, RollSoundTracking);
				}

				break;

			case 3: //Skid to a stop
				SetFrame(0, 6, PhaseOneProfile);
				NPC.rotation = 0;
				NPC.velocity.X *= 0.92f;

				float floorHeightAgain = FindGroundFromPositionIgnorePlatforms(NPC.Center).Y;
				//Match floor height when the slope is low enough
				if (floorHeightAgain > NPC.Top.Y && floorHeightAgain < NPC.Bottom.Y + 40)
				{
					NPC.velocity.Y = 0;
					NPC.position.Y = MathHelper.Lerp(NPC.position.Y, floorHeightAgain - NPC.height, 0.3f);
				}
				else if (!OnTopOfTiles)
					NPC.velocity.Y += 0.3f;

				if (Math.Sign(NPC.velocity.X) is int newDirection && newDirection != 0)
					NPC.direction = -newDirection;

				if (!Main.dedServ && Math.Abs(NPC.velocity.X) > 1 && Main.rand.NextBool())
				{
					Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Center) + Vector2.UnitY * 10);
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
					NPC.velocity.X /= 2;
					return GoBackToIdle();
				}

				break;
		}

		return 1f;

		bool RollSoundTracking(ActiveSound soundInstance)
		{
			if (CurrentState == AIState.Roll)
				soundInstance.Position = NPC.Center;
			return true;
		}
	}

	public void KickupDust(Vector2 pos, Vector2 velocity, ParticleLayer drawLayer = ParticleLayer.AboveNPC)
	{
		if (Main.dedServ)
			return;

		ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity, new Color(253, 239, 167) * 0.7f, Main.rand.NextFloat(0.05f, 0.25f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
		{
			Pixellate = true,
			DissolveAmount = 1,
			SecondaryColor = new Color(148, 138, 90) * 0.7f,
			TertiaryColor = new Color(118, 116, 66) * 0.7f,
			PixelDivisor = 3,
			ColorLerpExponent = 0.25f,
			Layer = drawLayer
		});

		Vector2 dustPosition = pos + Vector2.UnitY * 4f;
		Point tilePosition = dustPosition.ToTileCoordinates();
		int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

		Dust dust = Main.dust[dustIndex];
		dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-16f, 16f);
		dust.velocity = velocity;
		dust.noLightEmittence = true;
		dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
	}

	public void CreateRollParticles()
	{
		if (Main.dedServ)
			return;

		if (Main.rand.NextBool(2))
		{
			Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Bottom) + Vector2.UnitY * 10f);
			ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, new Vector2(-NPC.velocity.X * 0.3f, -2f), colors[0], Main.rand.NextFloat(0.05f, 0.25f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
			{
				Pixellate = true,
				DissolveAmount = 1,
				SecondaryColor = colors[1],
				TertiaryColor = colors[2],
				PixelDivisor = 3,
				ColorLerpExponent = 0.5f
			});
		}

		Vector2 dustPosition = NPC.Bottom + Vector2.UnitY * 3f;
		Point tilePosition = dustPosition.ToTileCoordinates();
		int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

		Dust dust = Main.dust[dustIndex];
		dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-16f, 16f);
		dust.velocity.Y = -Main.rand.NextFloat(1.5f, 4f);
		dust.velocity.X = -NPC.velocity.X * Main.rand.NextFloat(0.2f, 1f);
		dust.noLightEmittence = true;
		dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
	}
	#endregion

	#region Shockwave Slam
	public float ShockwaveAttack(ref bool retarget)
	{
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.velocity.X *= 0.8f;

		retarget = false;
		if (Counter < 5)
		{
			retarget = true;
			NPC.FaceTarget();
		}

		int lastFrameY = currentFrame.Y;
		int framerate = 10;
		//Faster telegraph if the player is going past scarab
		if (lastFrameY < 9)
			framerate += (int)(10 * Utils.GetLerpValue(100, -30, (Target.Center.X - NPC.Center.X) * NPC.direction, true));

		if (lastFrameY == 2 && ExtraMemory < 1)
		{
			SoundEngine.PlaySound(ChitterSound, NPC.Center);
			ExtraMemory++;
		}

		if (lastFrameY == 5 && ExtraMemory < 2)
		{
			SoundEngine.PlaySound(GroundPoundFallSound with { Volume = 1.2f, Pitch = 1f + 0.01f * framerate }, NPC.Center);
			ExtraMemory++;
		}

		FrameState updateResult = UpdateFrame(7, framerate, PhaseOneProfile, false);

		if (lastFrameY < 9 && currentFrame.Y >= 9)
		{
			ExtraMemory = 0;
			dealContactDamage = true;
			//projectiles and sfx here

			if (Main.netMode != NetmodeID.MultiplayerClient)
				SpawnShockwaveFissure();

			if (!Main.dedServ)
			{
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 17, 6, 65, 1000));
				Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
				ScarabHeatHazeShaderData.HeatHazeIntensity = 1f;
				SoundEngine.PlaySound(GroundPoundSlamSound, NPC.Center);
			}
		}

		if (updateResult == FrameState.Stopped)
			return GoBackToIdle();

		return 1f;
	}

	public void SpawnShockwaveFissure()
	{
		Vector2 fissurePos = FindGroundFromPositionIgnorePlatforms(NPC.Center);

		bool invalidFissurePosition = false;
		const float min_travel_distance = 512;
		const float big_burst_area = 128;
		const float max_travel_distance = 1800;

		float overshootMult = 0.3f + 0.7f * Utils.GetLerpValue(2f, 5f, Math.Abs(Target.velocity.X), true);
		float travelDistLeft = Math.Clamp(Math.Abs(NPC.Center.X - Target.Center.X) + big_burst_area * 3 * overshootMult, min_travel_distance + big_burst_area, max_travel_distance);

		float fullTravelDist = travelDistLeft;
		float travelspeed = min_travel_distance / (fullTravelDist - big_burst_area);
		float burstSpawnDelay = 5f;
		
		//Spawn a tile wave in the direction of the attack
		BouncingTileWave(NPC.direction, 45, 14, 60);

		while (travelDistLeft > 0)
		{
			//The big burst has its shockwave projectiles more closely packed
			float spacing = travelDistLeft <= big_burst_area ? 16 : 32;
			float vfxVelocity = travelDistLeft <= big_burst_area ? 0.8f : 1.2f;

			//Shockwave increases in height as it travels before getting even bigger at the burst point
			float travelProgress = Utils.GetLerpValue(fullTravelDist, big_burst_area, travelDistLeft, true);
			float shockwaveHeight = MathHelper.Lerp(50, 80, travelProgress);
			if (travelDistLeft <= big_burst_area)
				shockwaveHeight += Utils.Remap(travelDistLeft, big_burst_area, 0, 80, 120, true);

			//Progress linearly
			if (travelDistLeft > big_burst_area)
				burstSpawnDelay += travelspeed;

			//Kaboom!
			if (!invalidFissurePosition)
				Projectile.NewProjectile(NPC.GetSource_FromThis(), fissurePos, new Vector2(NPC.direction * vfxVelocity, 0f), ModContent.ProjectileType<SandShockwavePillar>(), GetProjectileDamage(STAT_SLAM_SHOCKWAVE_DAMAGE), 3, Main.myPlayer, burstSpawnDelay, shockwaveHeight);

			Vector2 newFissurePos = FindGroundFromPositionIgnorePlatforms(fissurePos + new Vector2(spacing * NPC.direction, -40), Math.Max(Target.Center.Y - 40, fissurePos.Y - 140));

			//If we do too big a vertical jump between positions, first we try to ignore it and if for 2x in a row the elevation diff is too big, we stop the shockwave early 
			if (Math.Abs(newFissurePos.Y - fissurePos.Y) > 200)
			{
				//If we already had a broken position last time, it's over, give up
				if (invalidFissurePosition)
					break;

				invalidFissurePosition = true;
				newFissurePos.Y = fissurePos.Y; //Go to the old Y level
			}
			else
				invalidFissurePosition = false;
			fissurePos = newFissurePos;

			//If we transition from the small fissure spreading across the floor to the bigger burst at the end, add an extra delay for impact
			if (travelDistLeft > big_burst_area && travelDistLeft - spacing <= big_burst_area)
				burstSpawnDelay += 17f;

			travelDistLeft -= spacing;
		}
	}
	#endregion

	#region Ground pound
	private int GroundPoundBounceCount => phaseTwo ? 3 : 1;

	public float GroundPoundAttack(ref bool retarget)
	{
		retarget = false;
		int max_bounces = GroundPoundBounceCount;
		const int final_bounce_track_time = 40;
		const int air_pause_time = 16;
		const int rest_time = 90;
		const float downwardsSlamGravity = 0.38f;
		ref float bounceIndex = ref NPC.ai[2];
		float artificialGravityMultiplier = 1f;

		NPC.noTileCollide = true;
		NPC.noGravity = true;

		bool onTheFloor = OnTopOfTiles && NPC.velocity.Y >= 0;

		if (bounceIndex == 0 && Counter == 0 && phaseTwo && NPC.velocity.Y >= -9)
			NPC.velocity.Y = Math.Min(-3, NPC.velocity.Y - 9f);

		float targetDistanceX = Math.Abs(Target.Center.X -  NPC.Center.X);
		if (bounceIndex > 0 && targetDistanceX < 300)
		{
			float extraAllowedHeight = Utils.GetLerpValue(40f, 300f, targetDistanceX, true);
			onTheFloor &= NPC.Center.Y >= Target.Center.Y - 16 - extraAllowedHeight * 200;
		}

		if (bounceIndex < max_bounces)
		{
			//Rolling up from the skies in phase 2
			if (phaseTwo && bounceIndex == 0)
			{
				if (currentFrame != RollFrame && UpdateFrame(3, 16, PhaseTwoProfile, false) == FrameState.Stopped)
				{
					SetFrame(RollFrame, PhaseTwoProfile);
					NPC.rotation += NPC.direction;
				}
			}
			else
				SetFrame(RollFrame, phaseTwo ? PhaseTwoProfile : PhaseOneProfile);

			dealContactDamage = true;

			//Bounce
			if (onTheFloor)
				GroundPoundBounce(ref bounceIndex, downwardsSlamGravity);
			else if (currentFrame == RollFrame)
				GroundPoundSpin(true);
		}

		else if (bounceIndex == max_bounces)
		{
			if (phaseTwo && Math.Abs(NPC.velocity.X) > 0)
				trailOpacity = Utils.Clamp(NPC.velocity.X / 4f, 0f, 0.3f);

			if (Counter == 1)
			{
				if (!Main.dedServ)
				{
					for (int i = 0; i < 3; i++)
					{
						Vector2 pos = NPC.BottomLeft;
						if (NPC.direction == -1)
							pos = NPC.BottomRight;

						pos.X += Main.rand.NextFloat(-20, 20);

						KickupDust(pos, new Vector2(-0.5f * NPC.direction, -1f).RotatedByRandom(0.25f) * Main.rand.NextFloat(1, 3));

						KickupDust(pos, new Vector2(0.05f * NPC.direction, -2f).RotatedByRandom(0.05f) * Main.rand.NextFloat(1, 5));
					}
				}

				SoundEngine.PlaySound(SmallChitterSound, NPC.Center);
				SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);
			}

			dealContactDamage = true;

			//Continue tracking in the air for a bit
			if (NPC.velocity.Y < 0)
			{
				GroundPoundSpin(true);
				NPC.rotation += NPC.velocity.X * 0.025f;
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
				VisualProfile profile = PhaseOneProfile;
				if (phaseTwo)
				{
					profile = PhaseTwoProfile;
					if (NPC.velocity.Y > 6)
						frame = new Point(4, 2);
				}

				if (NPC.velocity.Y > 8f)
				{
					trailOpacity = Utils.Clamp(MathHelper.Lerp(0f, 1f, (NPC.velocity.Y - 8f) / 14f), 0, 1) * 0.3f;
				}

				//Do the fall sound when we transition away from the roll frame
				if (currentFrame.X != frame.X && (currentFrame.X != 0))
					SoundEngine.PlaySound(GroundPoundFallSound, NPC.Center, GroundPoundFallSoundTrack);

				SetFrame(frame, profile);
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
					ScarabHeatHazeShaderData.HeatHazeIntensity = 1f;
					SoundEngine.PlaySound(GroundPoundSlamSound, NPC.Center);
					GroundImpactVFX(1f);

					for (int i = -10; i < 10; i++)
					{
						KickupDust(NPC.Bottom + new Vector2(8 * i, 0f), -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(5f));
					}
				}

				for (int i = -1; i <= 1; i += 2)
				{
					for (int j = 0; j < 4; j++)
					{
						float distStep = (200 + j * 56) * i;
						Vector2 projPosition = FindGroundFromPositionIgnorePlatforms(NPC.Bottom + Vector2.UnitX * distStep);
						Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.UnitY * i * 0.5f, ModContent.ProjectileType<SandShockwavePillar>(), GetProjectileDamage(STAT_GROUNDPOUND_SHOCKWAVE_DAMAGE), 3, Main.myPlayer, 1 + j * 3, 300 - j * 40f);
					}
				}
			}
		}

		else //rest before next attack
		{
			bool jumped = currentFrame == new Point(0, 2) && Profile == PhaseTwoProfile;

			if (!phaseTwo)
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
			else if (!jumped)
			{
				NPC.velocity *= 0.5f;
				NPC.rotation = 0;

				NPC.noGravity = false;
				NPC.noTileCollide = false;

				artificialGravityMultiplier = 0f;

				if (currentFrame.Y < 8)
					currentFrame.Y = 8;

				if (UpdateFrame(5, 16, PhaseTwoProfile, false) == FrameState.Stopped)
				{
					if (!Main.dedServ)
					{
						for (int i = 0; i < 20; i++)
						{
							Vector2 pos = NPC.Bottom;

							pos.X += Main.rand.Next(-30, 30);

							KickupDust(pos, new Vector2(0, -2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(1, 5), ParticleLayer.BelowSolid);

							KickupDust(pos, new Vector2(0, -1f).RotatedByRandom(1.5f) * Main.rand.NextFloat(1, 3));
						}
					}

					SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);

					SetFrame(0, 2, PhaseTwoProfile);
					NPC.velocity.Y -= 25;
					NPC.velocity.X += NPC.direction * 5;
					Counter = 0;
					NPC.noTileCollide = true;
					NPC.noGravity = true;
				}
			}
			else
			{
				NPC.dontTakeDamage = false;
				NPC.velocity.Y *= 0.95f;

				trailOpacity = EaseBuilder.EaseQuinticIn.Ease(1f - Counter / 20f);

				if (Counter >= 20)
					return GoBackToIdle();
			}
		}

		NPC.velocity.Y += downwardsSlamGravity * artificialGravityMultiplier;
		return 1f;
	}

	bool GroundPoundFallSoundTrack(ActiveSound sound)
	{
		sound.Position = NPC.Center;
		if (NPC.ai[2] > GroundPoundBounceCount)
			return false;
		return true;
	}

	void GroundPoundSpin(bool useYVelocity = false)
	{
		if (useYVelocity)
			NPC.rotation += NPC.direction * (0.25f + Math.Abs(NPC.velocity.Y) * 0.02f);
		else
			NPC.rotation += NPC.direction * 0.25f + NPC.velocity.X / 120;
	}

	void GroundPoundBounce(ref float bounceIndex, float downwardsSlamGravity)
	{
		bounceIndex++;
		//Avoid scenarios where scarab ends up stuck in the floor
		ShiftUpToFloorLevel();

		NPC.TargetClosest();
		NPC.FaceTarget();
		Counter = 0;

		Vector2 bounceTarget = FindGroundFromPositionIgnorePlatforms(Target.Center);
		bounceTarget.Y = Math.Min(bounceTarget.Y, Target.Center.Y + 300);
		bounceTarget += Target.velocity * 30f;

		float overshootMultiplier = Utils.GetLerpValue(1f, 3f, Target.velocity.X * NPC.direction, true) * 0.8f;
		float maxOvershootDistance = 200;
		float maxBounceXVel = 26f;

		if (bounceIndex == GroundPoundBounceCount)
		{
			overshootMultiplier = 2.5f;
			maxOvershootDistance = 600;
			maxBounceXVel = 36f;
		}

		bounceTarget.X += Math.Clamp(Target.Center.X - NPC.Center.X, -maxOvershootDistance, maxOvershootDistance) * overshootMultiplier;

		NPC.velocity = ArcVelocityHelper.GetArcVel(NPC.Center, bounceTarget, downwardsSlamGravity, minArcHeight: 300f, heightAboveTarget: 300f, maxXvel: maxBounceXVel);
		squishY = 0.6f;

		if (!Main.dedServ && (bounceIndex > 1 || phaseTwo))
		{
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 4, 15, 1800));
			Collision.HitTiles(NPC.BottomLeft, new Vector2(0, -6), NPC.width, 10);
			SoundEngine.PlaySound(BounceSound, NPC.Center);
			GroundImpactVFX(Math.Abs(NPC.velocity.Y) * 0.1f);
		}
	}
	#endregion

	#region Dig
	public float DigAttack(ref bool retarget)
	{
		const int dig_time = 120;
		ref float digState = ref NPC.ai[2];

		float initialJumpHeight = 0;
		float initialJumpSpeed = 12;
		float maxFallTimeBeforeDigDissapear = 50;
		float digDownVelocity = 0.2f;

		NPC.behindTiles = digState > 0;
		
		if (phaseTwo)
		{
			initialJumpSpeed = 8;
			initialJumpHeight = 8;
			maxFallTimeBeforeDigDissapear = 300;
			digDownVelocity = 0.5f;
			NPC.noTileCollide = true;
			NPC.noGravity = true;
		}

		switch (digState)
		{
			//Anticipation before the dig
			case 0:
				//There's a whole animation for the anticipation when in phase 1, but in phase 2 its done from the air so its skipped
				if (!phaseTwo && UpdateFrame(3, 12, PhaseOneProfile, false) != FrameState.Stopped)
				{
					NPC.velocity.X *= 0.95f;
					NPC.noGravity = false;
					NPC.noTileCollide = false;
				}
				else
				{
					if (!phaseTwo)
					{
						if (!Main.dedServ)
						{
							for (int i = 0; i < 12; i++)
							{
								Vector2 pos = NPC.BottomRight;
								if (NPC.direction == -1)
									pos = NPC.BottomLeft;

								pos.X += Main.rand.NextFloat(-60, 60);

								KickupDust(pos, new Vector2(-1f * NPC.direction, -1.2f).RotatedByRandom(1f) * Main.rand.NextFloat(2, 4), ParticleLayer.BelowSolid);
							}
						}

						SoundEngine.PlaySound(SmallChitterSound, NPC.Center);
						SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);
					}

					NPC.velocity.Y -= initialJumpHeight;
					NPC.velocity.X = NPC.direction * initialJumpSpeed;
					NPC.noTileCollide = true;
					NPC.noGravity = false;
					Counter = 0;
					digState++;
				}

				break;

			//Diving into the ground
			case 1:
				//In phase 1 scarab holds a "dive" frame
				if (!phaseTwo)
				{
					SetFrame(0, 1, PhaseOneProfile);
					NPC.rotation = NPC.velocity.Y * 0.05f * NPC.direction;
				}
				//In phase 2, since its leaping from above it rolls up into a ball
				else
				{
					//Rolling up
					if (currentFrame != RollFrame && UpdateFrame(3, 16, PhaseTwoProfile, false) == FrameState.Stopped)
					{
						SetFrame(RollFrame, PhaseTwoProfile);
						NPC.rotation += NPC.direction;
					}
					//balling
					else if (currentFrame == RollFrame)
						GroundPoundSpin();
				}

				NPC.velocity.Y += digDownVelocity;
				NPC.GravityMultiplier *= 2;

				if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height - 24) || Counter > maxFallTimeBeforeDigDissapear || NPC.Opacity != 1)
				{
					//Screenshake when it hits the floor in phase 2 because it did so from a high height
					if (!Main.dedServ && NPC.Opacity == 1 && phaseTwo)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 7, 10, 40, 900));
						SoundEngine.PlaySound(BounceSound, NPC.Center);
						GroundImpactVFX(Math.Abs(NPC.velocity.Y) * 0.3f);

						for (int i = 0; i < 24; i++)
						{
							Vector2 pos = NPC.BottomRight;
							if (NPC.direction == -1)
								pos = NPC.BottomLeft;

							pos.X += Main.rand.NextFloat(-60, 60);

							KickupDust(pos, new Vector2(-2f * NPC.direction, -1.2f).RotatedByRandom(1f) * Main.rand.NextFloat(2, 4));
						}
					}

					if ((NPC.Opacity -= 0.15f) <= 0)
					{
						digState++; //Disappear into the ground
						NPC.dontTakeDamage = true;
						Counter = 0;
					}

					//If were despawning
					if (CurrentState == AIState.Despawn)
					{
						NPC.active = false;
						return 0f;
					}
				}

				break;

			//Digging
			case 2: 
				NPC.Opacity = 0;
				NPC.noGravity = true;
				NPC.velocity = Vector2.Zero;
				retarget = false;

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

					//About to emerge!
					if (Counter >= dig_time - 30)
					{
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY.RotatedByRandom(1f), 6.5f, 3, 30, uniqueIdentity: "ScarabeusDigRumble"));
					}
				}

				if (Counter > dig_time) //Reemerge
				{
					Counter = 0;
					digState++;
					NPC.dontTakeDamage = false; 

					//Attack ends early in phase 2, with scarabeus transitionning from the dig immediately into a singular ground pound
					if (phaseTwo)
					{
						ChangeState(AIState.GroundPound);
						NPC.Opacity = 1;
						NPC.ai[2] = GroundPoundBounceCount - 1;
						GroundPoundBounce(ref NPC.ai[2], 0.38f);
						DigProjectileBurst();
						SetFrame(RollFrame, PhaseTwoProfile);
						NPC.noTileCollide = true;
						NPC.noGravity = true;
						if (!Main.dedServ)
							GroundImpactVFX(1.3f);
						return 0f;
					}
					else
					{
						if (NPC.DistanceSQ(Target.Center) < 50 * 50)
							NPC.velocity.Y -= 10;
						else
							NPC.velocity = NPC.GetArcVel(Target.Center + Vector2.Normalize(Target.velocity) * 400, NPC.gravity, 15, true);

						NPC.noGravity = false;
						NPC.direction = Math.Sign(NPC.velocity.X);

						if (!Main.dedServ)
						{
							for (int i = 0; i < 12; i++)
							{
								Vector2 pos = NPC.Center;

								pos.X += Main.rand.NextFloat(-30, 30);

								KickupDust(pos, new Vector2(1f * NPC.direction, -1.2f).RotatedByRandom(0.5f) * Main.rand.NextFloat(3, 6), ParticleLayer.AboveNPC);

								KickupDust(pos, new Vector2(1.5f * NPC.direction, -1.2f).RotatedByRandom(1f) * Main.rand.NextFloat(4, 8), ParticleLayer.BelowSolid);
							}
						}

						SoundEngine.PlaySound(ChitterSound, NPC.Center);
						SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);
					}
				}

				break;

			case 3: //Emerge and land

				retarget = false;
				NPC.rotation = NPC.velocity.Y * 0.08f * NPC.direction;
				NPC.Opacity = Math.Min(NPC.Opacity + 0.1f, 1);
				NPC.GravityMultiplier *= 2;

				dealContactDamage = (currentFrame.X == 2 && currentFrame.Y >= 6 && currentFrame.Y < 9) || (currentFrame.X != 2 && NPC.velocity.Y < 0);

				if ((NPC.Center.X - Target.Center.X) * NPC.direction > 0)
					NPC.velocity.X *= 0.96f;

				if (Counter > 10 && NPC.velocity.Y >= 0)
				{
					NPC.noTileCollide = false;
					if (OnTopOfTiles) //Land
					{
						NPC.velocity.X *= 0.1f;
						NPC.rotation = 0;
						NPC.behindTiles = false;

						if (currentFrame.Y < 5)
						{
							SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);
							SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);

							if (!Main.dedServ)
							{
								for (int i = 0; i < 4; i++)
								{
									Vector2 pos = NPC.BottomRight;
									if (NPC.direction == -1)
										pos = NPC.BottomLeft;

									pos.X += Main.rand.NextFloat(-4, 4);

									KickupDust(pos, new Vector2(2f * NPC.direction, -2.1f).RotatedByRandom(0.15f) * Main.rand.NextFloat(1, 4), ParticleLayer.AboveNPC);
								}
							}

							GroundImpactVFX(1.2f);
							currentFrame.Y = 5;
						}

						if (UpdateFrame(2, 12, PhaseOneProfile, false) == FrameState.Stopped)
							return GoBackToIdle();
						return 1f;
					}
				}

				break;
		}

		return 1f;
	}

	public void DigProjectileBurst()
	{
		if (DifficultyScale < 2 || Main.netMode == NetmodeID.MultiplayerClient)
			return;

		int projectileType = ModContent.ProjectileType<SandballProjectile>();

		Vector2 ground = FindGroundFromPositionIgnorePlatforms(NPC.Top);
		Point groundPos = ground.ToTileCoordinates();

		//TextureColorCache.GetDominantPalette(TextureAssets.Tile[Main.tile[groundPos].TileType].Value);

		for (int j = -2; j < 2; j++)
		{
			if (j == 0)
				continue;

			//1 less projectile at the side scarab emerges
			if (Math.Abs(j) == 2 && Math.Sign(j) == Math.Sign(NPC.velocity.X))
				continue;

			Vector2 velocity = -Vector2.UnitY.RotatedBy(j * 0.25f + Main.rand.NextFloat(-0.12f, 0.12f)) * Main.rand.NextFloat(9f, 11f);

			for (int i = 0; i < 4; i++)
			{
				KickupDust(ground, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(), ParticleLayer.AboveNPC);
			}

			Projectile.NewProjectile(NPC.GetSource_FromThis(), ground, velocity, projectileType, GetProjectileDamage(STAT_DIG_EMERGE_DEBRIS_DAMAGE), 3, Main.myPlayer, groundPos.X, groundPos.Y);
		}		
	}
	#endregion

	//Phase 2
	#region Flying dash
	public float SwoopDashAttack(ref bool retarget)
	{
		const float teleraph_time = 40;
		const float recovery_time = 20;
		const float idealXDistanceToPlayer = 500f;
		float distXToTarget = Math.Abs(NPC.Center.X - Target.Center.X);

		ref float dashState = ref NPC.ai[2];
		ref float dashRotation = ref NPC.ai[3];
		retarget = dashState < 1;

		NPC.noTileCollide = true;
		dealContactDamage = dashState == 1;

		if (Counter == 0 && dashState == 0)
		{
			SoundEngine.PlaySound(ChitterSound, NPC.Center);
			SoundEngine.PlaySound(GroundPoundFallSound, NPC.Center);

			NPC.FaceTarget();
			NPC.velocity.X = -NPC.direction * (3f + 6f * Utils.GetLerpValue(idealXDistanceToPlayer, idealXDistanceToPlayer * 0.3f, distXToTarget, true));
			NPC.velocity.Y -= 1;
		}

		switch (dashState)
		{
			//Anticipation
			case 0:
				UpdateFrame(2, 8 + (int)(Counter / teleraph_time * 12), PhaseTwoProfile);

				if (distXToTarget > idealXDistanceToPlayer)
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.01f);
				else
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (NPC.Center.X - Target.Center.X) < 0 ? -13 : 13, 0.034f);

				NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, 0f, 0.02f);
				NPC.rotation = NPC.velocity.X * 0.05f - MathF.Sin(Counter / (float)teleraph_time * 3.25f) * 0.3f * NPC.direction;

				if (Counter > teleraph_time && distXToTarget > idealXDistanceToPlayer * 0.9f)
				{
					dashState++;
					Counter = 0f;
					NPC.FaceTarget();

					//We do some funny buisness and use an arc trajectory and then flip the gravity around, so instead of doing ballistics up it does ballistics down. haha
					float distToPlayerX = (Target.Center.X - NPC.Center.X);
					if (Math.Abs(distToPlayerX) < 200)
						distToPlayerX = 200 * (distToPlayerX < 0 ? -1 : 1);
					Vector2 ballisticTarget = new Vector2(Target.Center.X + distToPlayerX, NPC.Center.Y);
					float targetHeight = FindGroundFromPositionIgnorePlatforms(Target.Center).Y;

					NPC.velocity = ArcVelocityHelper.GetArcVel(NPC.Center, ballisticTarget, 0.6f, heightAboveTarget: Math.Abs(NPC.Center.Y - targetHeight));
					NPC.velocity.Y *= -1;
					if (Math.Abs(NPC.velocity.X) < 9)
						NPC.velocity.X = (NPC.velocity.X < 0 ? -1 : 1) * 9;
				}

				break;

			//Dash
			case 1:
				
				if (Main.rand.NextBool())
				{
					Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2);

					ParticleHandler.SpawnParticle(new GlowParticle(
						pos,
						-NPC.velocity * 0.1f, 
						Color.Orange.Additive(), 
						0.5f, 
						30)
						);

					ParticleHandler.SpawnParticle(new GlowParticle(
						pos,
						-NPC.velocity * 0.1f,
						Color.White.Additive(),
						0.4f,
						20)
						);
				}

				SetFrame(0, 0, PhaseTwoProfile);

				if (Counter < 30)
					trailOpacity = 1f - Counter / 30f;

				//Fake the velocity accelerating over time WITHOUT actually multiplying it
				//cuz thatd be too annoying and mess up the ballistics so we actually move scarab back a portion of its speed
				float speedMultiplier = 0.1f + Math.Min(1, Counter / 14f) * 1f;
				NPC.position -= NPC.velocity * (1 - speedMultiplier);
				NPC.velocity.Y -= 0.6f * speedMultiplier;

				NPC.rotation = NPC.velocity.ToRotation() + ((NPC.direction == -1) ? MathHelper.Pi : 0);

				if (NPC.velocity.Y < 0)
					NPC.velocity.X *= 0.98f;

				if (NPC.velocity.Y < -4)
				{
					if (Main.rand.NextBool())
						return TransitionIntoRoll();

					Counter = 0;
					NPC.velocity.X *= 0.7f;
					dashState++;
				}

				break;

			case 2:
				float worthless = 1f;
				FlyHover(ref worthless, 0.3f + 0.7f * Counter / recovery_time);
				if (Counter > recovery_time)
					return GoBackToIdle();
				break;
		}
		
		return 1f;
	}

	private float TransitionIntoRoll()
	{
		ChangeState(AIState.Roll);
		NPC.velocity = NPC.velocity.RotatedBy(-0.45f * NPC.direction);
		if (NPC.velocity.Length() > 23f)
			NPC.velocity = NPC.velocity.SafeNormalize(-Vector2.UnitY) * 23f;

		//NPC.velocity.X = Math.Clamp(NPC.velocity.X, -5, 5);
		NPC.position.Y -= 20;
		NPC.noTileCollide = true;
		NPC.noGravity = false;
		SetFrame(RollFrame, PhaseTwoProfile);
		return 0f;
	}
	#endregion

	#region Swarm
	public float SwarmAttack(ref bool retarget)
	{
		//durations of each segment
		const float swarm_length = 340;
		const float attack_end_time = swarm_length + 180;
		const int projectileSpawnDelay = 40;

		NPC.noGravity = true;
		NPC.noTileCollide = true;

		if (Math.Abs(NPC.Center.X - Target.Center.X) > 90f)
			NPC.FaceTarget();

		ScarabHeatHazeShaderData.HeatHazeTargetIntensity = 0.6f * Math.Min(1, Counter / 150f);

		//attack start
		if (Counter == 0)
		{
			NPC.velocity.Y = -6;
			NPC.velocity.X /= 2;
			NPC.FaceTarget();

			if(!Main.dedServ)
				ParticleHandler.SpawnParticle(new LensFlareRing(NPC.Center - Vector2.UnitY * 60, 0.3f, 600, 60, EaseFunction.EaseCircularOut).Attach(NPC));
		}

		//Try to hover at approx the same height above the player
		float targetHeight = FindGroundFromPositionIgnorePlatforms(Target.position).Y - 360;
		float distanceToTarget = Math.Abs(NPC.Center.Y - targetHeight);
		float speedToTarget = Utils.GetLerpValue(10, 210f, distanceToTarget, true) * 3f;
		NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, Math.Sign(targetHeight - NPC.Center.Y) * speedToTarget, 0.04f);

		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 4f, 0.02f);

		UpdateFrame(1, 12, PhaseTwoProfile);
		NPC.rotation = NPC.velocity.X * 0.05f;

		//if (Counter < 360)
			//trailOpacity = 0.4f * (1f - Counter / 360f);

		SwarmAttackVisuals();

		if (Counter < swarm_length && Counter % projectileSpawnDelay == 0)
			SpawnBabySwarmer((int)(Counter / projectileSpawnDelay));

		if (Counter >= attack_end_time)
			return GoBackToIdle();

		return 1f;
	}

	public void SpawnBabySwarmer(int swarmerIndex)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		float spawnAreaOffsetX = Target.velocity.X * 35f;
		float spawnAreaRadius = 400 - swarmerIndex % 4 * 30;

		//if (Main.rand.NextBool(3))
		//	spawnAreaRadius = 0f;

		Vector2 spawnPosition = Target.Center + Vector2.UnitX * (spawnAreaOffsetX + Main.rand.NextFloat(-spawnAreaRadius, spawnAreaRadius));
		spawnPosition = FindGroundFromPositionIgnorePlatforms(spawnPosition);

		float spawnHopHeight = 5.6f + 2.3f * (swarmerIndex % 3);
		Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<BabyAntlionProjectile>(), GetProjectileDamage(STAT_ANTLION_SWARMER_DAMAGE), 0, Main.myPlayer, NPC.whoAmI, spawnHopHeight);

		//Spawn swarmers further away that are only just meant to make it look more natural
		if (swarmerIndex % 2 == 1)
		{
			spawnHopHeight = 7f;
			spawnPosition = Target.Center + Vector2.UnitX * (spawnAreaOffsetX + (Main.rand.NextBool() ? -1 : 1) * spawnAreaRadius * 1.4f);
			spawnPosition = FindGroundFromPositionIgnorePlatforms(spawnPosition);
			Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<BabyAntlionProjectile>(), GetProjectileDamage(STAT_ANTLION_SWARMER_DAMAGE), 0, Main.myPlayer, NPC.whoAmI, spawnHopHeight);
		}

		if (swarmerIndex % 3 == 2)
		{
			spawnPosition = Target.Center + Vector2.UnitX * (-spawnAreaOffsetX * 0.5f + Main.rand.NextFloat(-spawnAreaRadius, spawnAreaRadius));
			spawnPosition = FindGroundFromPositionIgnorePlatforms(spawnPosition);
			spawnHopHeight = 1 + 1.3f * (swarmerIndex % 3);
			Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<BabyAntlionProjectile>(), GetProjectileDamage(STAT_ANTLION_SWARMER_DAMAGE), 0, Main.myPlayer, NPC.whoAmI, spawnHopHeight);

		}
	}

	public void SwarmAttackVisuals()
	{
		if (Main.dedServ)
			return;

		Vector2 orbPosition = NPC.Center + new Vector2(0f, -60f);

		if (Main.rand.NextBool(10))
			ParticleHandler.SpawnParticle(new EmberParticle(orbPosition + Main.rand.NextVector2Circular(50f, 50f), -Vector2.UnitY, Color.Orange, 1f, 30));
	}
	#endregion
}