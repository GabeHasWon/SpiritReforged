using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Desert.ScarabBoss.Dusts;
using SpiritReforged.Content.Desert.ScarabBoss.Gores;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	public bool FightingDScourge
	{
		get => desertScourge != null;
	}

	private int scourgeFightManagerIndex = -1;
	public ModNPC scourgeFightManager;
	private NPC _cachedDesertScourge;

	private static int duoFightManagerType = -1;
	private static int desertScourgeType = -1;

	public NPC desertScourge
	{
		get
		{
			if (!CrossMod.Fables.Enabled || scourgeFightManager == null)
				return null;

			if (_cachedDesertScourge == null || !_cachedDesertScourge.active)
				_cachedDesertScourge = (NPC)(CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "getDesertScourge", scourgeFightManager));

			return _cachedDesertScourge;
		}
	}

	public static object HandleModCall(object[] args)
	{
		//???
		if (args.Length < 2 || args[1] is not string instruction)
			return null;

		switch (instruction)
		{
			case "doDigRumbleVFX":
				DuoFightSpawnRumbleVFX((Rectangle)args[2], (bool)args[3]);
				return true;
			case "getScarabHealth":
				return Main.masterMode ? STAT_LIFEMAX_MASTER : Main.expertMode ? STAT_LIFEMAX_EXPERT : STAT_LIFEMAX_NORMAL;
			case "isScarabOnTopOfTiles":
				return ((args[2] as NPC).ModNPC as Scarabeus).OnTopOfTiles;
			case "doScourgeLandSlamShockwave":
				((args[2] as NPC).ModNPC as Scarabeus).DuoFightGigaFloorShockwave(false, (Vector2)args[3]);
				return true;
			case "doGroundPoundShockwaves":
				float safeSpace = 200;
				int slamColumns = 4;
				float maxColumnHeight = 300;
				float heightLossPerStep = 40;
				bool doLightningStrikes = false;

				if (args.Length > 3)
					safeSpace = (float)args[3];
				if (args.Length > 4)
					slamColumns = (int)args[4];
				if (args.Length > 5)
					maxColumnHeight = (float)args[5];
				if (args.Length > 6)
					heightLossPerStep = (float)args[6];
				if (args.Length > 7)
					doLightningStrikes = (bool)args[7];

				((args[2] as NPC).ModNPC as Scarabeus).DoGroundPoundShockwaves(safeSpace, slamColumns, maxColumnHeight, heightLossPerStep, doLightningStrikes);
				return true;
			case "setFrame":
				VisualProfile profile = PhaseOneProfile;

				if (args.Length > 4)
				{
					int profileIndex = (int)args[4];
					switch (profileIndex)
					{
						case 1:
							profile = PhaseTwoProfile;
							break;
						case 2:
							profile = TakeoffProfile;
							break;
						case 3:
							profile = SimulatedProfile;
							break;
						case 4:
							profile = DeadProfile;
							break;
					}
				}

				((args[2] as NPC).ModNPC as Scarabeus).SetFrame((Point)args[3], profile);
				return true;
			case "renderScarab":
				((args[2] as NPC).ModNPC as Scarabeus).PreDraw((SpriteBatch)args[3], (Vector2)args[4], (Color)args[5]);
				return true;
			case "getPukedOut":
				((args[2] as NPC).ModNPC as Scarabeus).DuoFightGetPukedOut((Vector2)args[3], (Vector2)args[4]);
				return true;
			case "doBurnParticles":
				((args[2] as NPC).ModNPC as Scarabeus).DoBurnParticles((Vector2)args[3], (Vector2)args[4], (float)args[5]);
				return true;
			case "startJoustSwoop":
				((args[2] as NPC).ModNPC as Scarabeus).DuoFightStartAirDash((Vector2)args[3]);
				return true;
			case "spawnEltronGore":
				((args[2] as NPC).ModNPC as Scarabeus).DuoFightSpawnDeathGores((Vector2)args[3]);
				return true;				
		}

		return null;
	}

	#region Setup
	private static LocalizedText DuoFightHoverText;
	private void LoadDuoFight()
	{
		if (!CrossMod.Fables.Enabled)
			return;

		if (CrossMod.Fables.Instance.TryFind("DesertScourge", out ModNPC dscourge))
			desertScourgeType = dscourge.Type;
		if (CrossMod.Fables.Instance.TryFind("ScourgeVsScarab", out ModNPC duel))
			duoFightManagerType = duel.Type;
		DuoFightHoverText = CrossMod.Fables.Instance.GetLocalization("Extras.ScourgeVsScarabHover");
	}

	public void CheckDuoFightStart(IEntitySource source)
	{
		//If we've been spawned by the duo fight manager, use it as the lifemax
		if (source is EntitySource_Parent parentSource && parentSource.Entity is NPC parentNPC && parentNPC.type == duoFightManagerType)
		{
			NPC.realLife = parentNPC.whoAmI;
			scourgeFightManagerIndex = parentNPC.whoAmI;
			scourgeFightManager = parentNPC.ModNPC;
			CurrentState = AIState.DuoFightSpawnAnim;
			scarabColorIndex = 0; //No recolored scarab because at points in the fight a different shader gets applied on scarab so we can't rely on the iridescence shader for it
		}
	}
	#endregion

	#region Spawn anim
	public float DuoFightSpawnAnimation(ref bool retarget)
	{
		retarget = false;
		NPC.direction = 1; //Scarab will always jump from the left

		if (scourgeFightManager == null || !scourgeFightManager.NPC.active)
		{
			NPC.Opacity = 1f;
			ChangeState(AIState.IdleAwayFromPlayer);
			return 0f;
		}

		float spawnAnimProgress = (float)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "spawnAnimPercent", scourgeFightManager);

		NPC.Opacity = 1f;
		Vector2 targetPosition = (Vector2)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "spawnAnimPos", scourgeFightManager);
		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.behindTiles = spawnAnimProgress < 0.5f;
		NPC.velocity = targetPosition - NPC.Center;
		SetFrame(RollFrame, PhaseTwoProfile);

		float slowdown = 1 - spawnAnimProgress * 0.7f;
		trailOpacity = MathF.Pow(1 - slowdown, 0.5f);

		NPC.rotation += NPC.direction * 0.55f * slowdown;
		return 1f;
	}

	public float DuoFightSpawnFallback(ref bool retarget)
	{
		NPC.direction = -1; //Bounce back to the right
		retarget = false;
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.02f);
		NPC.velocity.Y += 0.35f;
		SetFrame(RollFrame, PhaseTwoProfile);
		NPC.rotation += NPC.direction * (0.3f + NPC.velocity.Y * 0.03f);

		NPC.noGravity = false;
		NPC.noTileCollide = true;

		//Hitting the ground
		if (NPC.velocity.Y > 0 && OnTopOfTiles)
		{
			NPC.rotation = 0;
			ShiftUpToFloorLevel();
			SoundEngine.PlaySound(BounceSound, NPC.Center);
			GroundImpactVFX(Math.Abs(NPC.velocity.Y) * 0.1f);
			NPC.velocity.Y *= 0;
			currentFrame.X = 0;
			Profile = PhaseOneProfile;
			ChangeState(AIState.IdleAwayFromPlayer);
			return 0f;
		}

		return 1f;
	}
	#endregion

	#region Takeoff from floor
	public float DuoFightTakeoff(ref bool retarget)
	{
		retarget = false;
		bool jumped = (currentFrame == new Point(0, 2) || currentFrame == new Point(0, 3)) && Profile == PhaseTwoProfile;
		bool jumpingIntoLightbulb = (bool)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "shouldTakeoffIntoLightbulb", scourgeFightManager);
		float towardsFightManager = (scourgeFightManager.NPC.Center.X - NPC.Center.X) < 0 ? -1 : 1;

		if (!jumped)
		{
			if (Counter == 0)
				ShiftUpToFloorLevel();

			NPC.velocity *= 0.5f;
			NPC.rotation = 0;
			NPC.noGravity = false;
			NPC.noTileCollide = false;

			if (currentFrame.Y < 10)
				currentFrame.Y = 10;
			if (currentFrame.Y is 19 or 21)
				SetFrame(0, currentFrame.Y + 1, TakeoffProfile);

			//Jump!!!
			if (UpdateFrame(0, 11, TakeoffProfile, false) == FrameState.Stopped)
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

				SetFrame(0, jumpingIntoLightbulb ? 2 : 3, PhaseTwoProfile);
				NPC.velocity.Y -= 25;

				if (jumpingIntoLightbulb)
					NPC.velocity.X += NPC.direction * 5;
				else
					NPC.velocity.X += towardsFightManager * 10;

				Counter = 0;
				NPC.noTileCollide = true;
				NPC.noGravity = true;
			}
		}
		else
		{
			NPC.velocity.Y *= 0.95f;
			trailOpacity = EaseFunction.EaseQuinticIn.Ease(1f - Counter / 20f);
			SetFrame(0, jumpingIntoLightbulb ? 2 : 3, PhaseTwoProfile);

			if (Counter >= 20)
			{
				if (FightingDScourge)
					CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "takeoffCompleted", scourgeFightManager);
				//Fallback just in case something horrible happened
				else
					ChangeState(AIState.Swarm);
			}
		}

		return 1f;
	}
	#endregion

	#region Giga Slam Attack
	public void DuoFightGigaFloorShockwave(bool unearthScourge = true, Vector2? shockwaveCenter = null)
	{
		Vector2 fissureCenterPos = shockwaveCenter ?? scourgeFightManager.NPC.Center;
		fissureCenterPos = FindGroundFromPositionIgnorePlatforms(fissureCenterPos);

		float scourgeTotalLength = (float)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "getScourgeLength", scourgeFightManager);
		float scourgeHalfWidth = scourgeTotalLength * 0.5f;

		float burstDelay = unearthScourge ? 7f : 1f;

		if (unearthScourge)
			CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "sendScourgeFlyingUp", scourgeFightManager, fissureCenterPos, burstDelay + 2);

		//Spawn a tile wave
		BouncingTileWave((int)(scourgeTotalLength / 19), 14, 60, null);

		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		for (int side = -1; side <= 1; side += 2)
		{
			bool invalidFissurePosition = false;
			float big_burst_area = scourgeTotalLength / 3f;
			Vector2 fissurePos = fissureCenterPos;

			float travelDist = 0f;

			float fullTravelDist = scourgeHalfWidth;
			float travelspeed = 0.1f;
			float burstSpawnDelay = burstDelay;

			while (travelDist < fullTravelDist)
			{
				//The big burst has its shockwave projectiles more closely packed
				float spacing = travelDist <= big_burst_area ? 16 : 32;

				//Shockwave increases in height as it travels before getting even bigger at the burst point
				float travelProgress = Utils.GetLerpValue(fullTravelDist, big_burst_area, travelDist, true);
				float shockwaveHeight = MathHelper.Lerp(50, 80, travelProgress);
				if (travelDist <= big_burst_area)
					shockwaveHeight += Utils.Remap(travelDist, 0, big_burst_area, 120, 40, true);

				float vfxVelocity = travelDist <= big_burst_area ? 0.1f : 1.5f - travelProgress;

				//Progress linearly
				if (travelDist > big_burst_area)
					burstSpawnDelay += travelspeed;

				//Kaboom!
				if (!invalidFissurePosition)
					Projectile.NewProjectile(NPC.GetSource_FromThis(), fissurePos, new Vector2(side * vfxVelocity, 0f), ModContent.ProjectileType<SandShockwavePillar>(), GetProjectileDamage(STAT_SLAM_SHOCKWAVE_DAMAGE), 3, Main.myPlayer, burstSpawnDelay, shockwaveHeight);

				Vector2 newFissurePos = FindGroundFromPositionIgnorePlatforms(fissurePos + new Vector2(spacing * side, -40), Math.Max(Target.Center.Y - 40, fissurePos.Y - 140));

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
				if (travelDist < big_burst_area && travelDist + spacing >= big_burst_area)
					burstSpawnDelay += 17f;
				travelDist += spacing;
			}
		}
	}

	#endregion

	#region Electrolunge with Scarab Attack
	public float DuoFightGrabbedByScourge(ref bool retarget)
	{
		retarget = false;
		if (scourgeFightManager == null)
			return GoBackToIdle();

		SetFrame(RollFrame, PhaseOneProfile);
		Vector3 positionInfo = (Vector3)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "getPositionInScourgeJaws", scourgeFightManager);
		NPC.Center = new Vector2(positionInfo.X, positionInfo.Y);
		NPC.rotation = positionInfo.Z;
		NPC.behindTiles = true;

		if (CurrentState == AIState.DuoFightEaten)
			NPC.Opacity = 0f;
		else
			NPC.Opacity = 1f;

		return 1f;
	}
	#endregion

	#region Attract Scarab then get eaten then get puked out
	public static Point GunkBallFrame = new Point(0, 9);

	public static float STAT_DUOFIGHT_PUKEROLL_GRAVITY = 0.47f;

	public void DoBurnParticles(Vector2 center, Vector2 impartedVelocity, float radius)
	{
		if (Main.dedServ)
			return;

		Lighting.AddLight(center, 0.6f, 0.2f, 0.2f);

		if (Main.rand.NextBool(3))
		{
			Vector2 position = center + Main.rand.NextVector2Circular(radius, radius);
			Vector2 velocity = -Vector2.UnitY + impartedVelocity;
			Color[] colors = [new Color(255, 200, 0, 100), new Color(255, 115, 0, 100), new Color(200, 3, 33, 100)];
			float scale = Main.rand.NextFloat(0.06f, 0.09f);
			int maxTime = (int)(Main.rand.Next(10, 35));

			ParticleHandler.SpawnParticle(new FireParticle(position, velocity, colors, 1.25f, scale, EaseFunction.EaseQuadOut, maxTime)
			{
				ColorLerpExponent = 2.5f
			});
		}

		if (Main.rand.NextBool(4))
		{
			var p = new EmberParticle(
				center + Main.rand.NextVector2Circular(radius, radius) * 1.4f,
				-impartedVelocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.3f),
				Color.Orange,
				Main.rand.NextFloat(0.2f, 0.5f),
				30
				);

			p.emitLight = false;

			ParticleHandler.SpawnParticle(p);
		}

		if (Main.rand.NextBool())
		{
			var p = new SmokeCloud(
				center + Main.rand.NextVector2Circular(radius, radius),
				-Vector2.UnitY.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.2f),
				Color.Black * 0.15f,
				0.175f,
				EaseFunction.EaseCircularOut,
				30);

			p.Pixellate = true;
			p.Layer = ParticleLayer.BelowProjectile;

			ParticleHandler.SpawnParticle(p);
		}
	}

	public void DuoFightSpawnSwarmersFar(int swarmerIndex)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		int swarmerCount = swarmerIndex % 2 == 1 ? 3 : 2;
		for (int i = 0; i < swarmerCount; i++)
		{
			//Spawn swarmers far away
			float spawnAreaDirection = (Main.rand.NextBool() ? -1 : 1);
			float spawnAreaOffsetX = 400;
			float spawnAreaRadius = 600 - swarmerIndex % 4 * 30;

			Vector2 spawnPosition = NPC.Center + Vector2.UnitX * (spawnAreaOffsetX + Main.rand.NextFloat(spawnAreaRadius)) * spawnAreaDirection;

			spawnPosition = FindGroundFromPositionIgnorePlatforms(spawnPosition);
			float spawnHopHeight = 5.6f + 2.3f * ((swarmerIndex + i) % 4);
			Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<BabyAntlionProjectile>(), GetProjectileDamage(STAT_ANTLION_SWARMER_DAMAGE), 0, Main.myPlayer, NPC.whoAmI, spawnHopHeight);
		}
	}

	public void DuoFightMicroBurnOnScourge()
	{
		CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "inflictScourgeMicroburn", scourgeFightManager);
	}

	public bool StandingStillWaitingToGetEatenByScourge
	{
		get
		{
			if (!FightingDScourge)
				return false;

			if (CurrentState == AIState.DuoFightDeathSwarm)
				return true;

			return (bool)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "swarmSitStill", scourgeFightManager);
		}
	}

	public void DuoFightGetPukedOut(Vector2 vomitPosition, Vector2 spitTarget)
	{
		NPC.Center = vomitPosition;
		NPC.Opacity = 1f;

		Player target = (Player)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "getScourgeTarget", scourgeFightManager);
		spitTarget.X += Math.Sign(target.Center.X - NPC.Center.X) * 400;

		Vector2 velocity =  ArcVelocityHelper.GetArcVel(NPC.Center, spitTarget, STAT_DUOFIGHT_PUKEROLL_GRAVITY, minArcHeight: 130f, maxXvel: 50f, heightAboveTarget: 100f);
		NPC.velocity = velocity;

		ChangeState(AIState.DuoFightGunkRoll);
		NPC.noTileCollide = true;
		NPC.noGravity = true;
		SetFrame(GunkBallFrame, PhaseOneProfile);
	}

	public void DuoFightPukesplosion()
	{
		CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "pukesplosion", scourgeFightManager);
	}
	#endregion

	#region Joust Dash
	public float DuoFightFlyFollowLeader(ref bool retarget)
	{
		NPC.target = scourgeFightManager.NPC.target;
		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.rotation = NPC.velocity.X * 0.05f;
		NPC.direction = Math.Sign(Target.Center.X - NPC.Center.X);
		wingFrameCounter += (18f / 60f); //18 fps for wings specifically
		UpdateFrame(0, 12, SimulatedProfile);

		float speedup = Utils.GetLerpValue(40f, 100f, NPC.Distance(scourgeFightManager.NPC.Center), true);
		float accel = Counter;
		if (accel < 0)
			accel = 45 + Counter;

		float movespeed = MathHelper.Lerp(4f, 16f + (accel / 40f) * 20f, speedup);
		float acceleration = MathHelper.Lerp(0.03f, 0.08f, MathF.Min(1f, speedup + accel / 16f));

		Vector2 toLeader = (scourgeFightManager.NPC.Center - NPC.Center).SafeNormalize(Vector2.Zero) * movespeed;
		NPC.velocity = Vector2.Lerp(NPC.velocity, toLeader, acceleration);

		if (Math.Sign(NPC.velocity.X) == Math.Sign(NPC.Center.X - scourgeFightManager.NPC.Center.X))
			NPC.velocity *= 0.97f;

		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -18, 18);
		NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -18, 18);
		return 1f;
	}

	public float DuoFightFlyDownToGround(ref bool retarget)
	{
		retarget = false;

		switch (ExtraMemory)
		{
			//Flying down to the ground
			case 0:
				NPC.noTileCollide = true;
				NPC.noGravity = true;
				NPC.rotation = NPC.velocity.X * 0.03f;
				wingFrameCounter += (18f / 60f); //18 fps for wings specifically
				UpdateFrame(0, 12, SimulatedProfile);

				NPC.velocity.X *= 0.98f;
				NPC.velocity.Y += 0.6f;
				if (NPC.velocity.Y > 12)
					NPC.velocity.Y = 12;

				if (OnTopOfTiles && NPC.velocity.Y >= 0)
				{
					ShiftUpToFloorLevel();
					ExtraMemory = 1;
					NPC.rotation = 0;
					NPC.velocity.Y = 0;
					NPC.velocity.X *= 0.6f;
					SetFrame(6, 0, PhaseOneProfile);
				}

				break;
			case 1:
				retarget = true;
				NPC.FaceTarget();

				NPC.noTileCollide = false;
				NPC.noGravity = false;

				NPC.rotation = 0;
				NPC.velocity.Y = 0;
				NPC.velocity.X *= 0.85f;
				if (UpdateFrame(6, 12, PhaseOneProfile, false) == FrameState.Stopped)
					return GoBackToIdle();
				break;
		}

		return 1f;
	}

	public void DuoFightStartAirDash(Vector2 dashTarget)
	{
		NPC.netUpdate = true;
		CurrentState = AIState.SwoopDash;
		ExtraMemory = 1;
		Counter = 0f;
		NPC.direction = (dashTarget.X - NPC.Center.X) < 0 ? -1 : 1;

		//We do some funny buisness and use an arc trajectory and then flip the gravity around, so instead of doing ballistics up it does ballistics down. haha
		float distToTargetX = (dashTarget.X - NPC.Center.X);
		float targetHeight = FindGroundFromPositionIgnorePlatforms(dashTarget).Y;

		//Cant go too low
		targetHeight = Math.Min(targetHeight, dashTarget.Y + 160);

		Vector2 ballisticTarget = new Vector2(dashTarget.X , targetHeight);
		if (Math.Abs(distToTargetX) < 200)
			ballisticTarget.X += 200 * NPC.direction;

		//Flip target upside down
		ballisticTarget.Y = (ballisticTarget.Y - NPC.Center.Y) * -1 + NPC.Center.Y;
		if (ballisticTarget.Y >= NPC.Center.Y - 100)
			ballisticTarget.Y = NPC.Center.Y - 100;

		NPC.velocity = ArcVelocityHelper.GetArcVel(NPC.Center, ballisticTarget, 0.6f);

		NPC.velocity.Y *= -1;
		if (Math.Abs(NPC.velocity.X) < 9)
			NPC.velocity.X = (NPC.velocity.X < 0 ? -1 : 1) * 9;
	}
	#endregion

	#region Roll -> Hit Scourge -> Bounce ground pounds
	public void DuoFightSimulateRoll(float rollSpeed)
	{
		Vector2 simulatedRollPosition = NPC.Center;
		Vector2 simulatedVelocity = NPC.velocity;
		Vector2 simulatedTargetPosition = Target.Center;
		Vector2 simulatedTargetVelocity = Target.velocity;

		simulatedTargetVelocity.X = Math.Clamp(simulatedTargetVelocity.X, -Target.maxRunSpeed, Target.maxRunSpeed);
		simulatedTargetVelocity.Y *= 0;

		float targetReachTime = 0f;

		for (int i = 0; i < 300; i++)
		{
			float npcTopY = simulatedRollPosition.Y - NPC.height / 2f;
			float npcBottomY = simulatedRollPosition.Y + NPC.height / 2f;

			float interpolant = 0f;
			float dist = Math.Abs(simulatedTargetPosition.X - simulatedRollPosition.X);
			if (dist > 300f)
				interpolant = 1f;
			else if (dist > 100f)
				interpolant = (dist - 100f) / 200f;

			float adjustedRollSpeed = rollSpeed * MathHelper.Lerp(1f, 1.5f, interpolant);

			simulatedVelocity.X = MathHelper.Lerp(simulatedVelocity.X, NPC.direction * adjustedRollSpeed, 0.1f);
			float floorHeight = FindGroundFromPositionIgnorePlatforms(simulatedRollPosition, Math.Max(simulatedRollPosition.Y - 14f, Target.Bottom.Y)).Y;

			//Match floor height when the slope is low enough
			if (floorHeight > npcTopY && floorHeight < npcBottomY + 40)
			{
				simulatedVelocity.Y = 0;
				simulatedRollPosition.Y = MathHelper.Lerp(simulatedRollPosition.Y, floorHeight - NPC.height * 0.5f, 0.6f);
			}
			//Fall if theres no floor
			else if (!SimOnTopOfTiles(simulatedRollPosition) && floorHeight > npcBottomY)
				simulatedVelocity.Y += 0.45f;

			//Bonk and transition to ground pound
			else if (floorHeight < npcTopY)
			{
				simulatedTargetPosition.X -= Math.Sign(simulatedVelocity.X) * 16f;
				break;
			}

			if ((simulatedTargetPosition.X - simulatedRollPosition.X) * NPC.direction < -500)
				break;

			simulatedRollPosition += simulatedVelocity;
			simulatedTargetPosition += simulatedTargetVelocity;
			targetReachTime++;
			//Dust.QuickDust(simulatedRollPosition, Color.Red);
		}

		CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "commitRollSimOutput", scourgeFightManager, simulatedRollPosition, targetReachTime);

		bool SimOnTopOfTiles(Vector2 simPosition)
		{
			Vector2 cachedPosition = NPC.Center;
			NPC.Center = simPosition;
			bool value = OnTopOfTiles;
			NPC.Center = cachedPosition;
			return value;
		}
	}

	public float DuoFightRollBonk(float downwardsSlamGravity = DEFAULT_GROUND_POUND_GRAVITY)
	{
		if (!Main.dedServ)
		{
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 6, 5, 35));
			Collision.HitTiles(NPC.TopLeft, NPC.velocity, NPC.width, NPC.height + 14);
			ScarabHeatHazeShaderData.HeatHazeIntensity = 0.7f;
		}

		float cachedRotation = NPC.rotation;
		ChangeState(AIState.GroundPound);
		NPC.rotation = cachedRotation;
		SetFrame(RollFrame, PhaseOneProfile);
		NPC.TargetClosest();
		NPC.FaceTarget();
		Counter = 1;
		ExtraMemory = 1;

		//Start a bounce
		Vector2 bounceTarget = FindGroundFromPositionIgnorePlatforms(Target.Center);
		bounceTarget.Y = Math.Min(bounceTarget.Y, Target.Center.Y + 300);
		bounceTarget += Target.velocity * 30f;

		float overshootMultiplier = Utils.GetLerpValue(1f, 3f, Target.velocity.X * NPC.direction, true) * 0.8f;
		float maxOvershootDistance = 200;
		float maxBounceXVel = 26f;
		bounceTarget.X += Math.Clamp(Target.Center.X - NPC.Center.X, -maxOvershootDistance, maxOvershootDistance) * overshootMultiplier;

		NPC.velocity = ArcVelocityHelper.GetArcVel(NPC.Center, bounceTarget, downwardsSlamGravity, minArcHeight: 300f, heightAboveTarget: 300f, maxXvel: maxBounceXVel);
		squishY = 0.6f;

		CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "rollBonk", scourgeFightManager, bounceTarget, DuoFightSimulateBounce(downwardsSlamGravity));
		return 0f;
	}

	public float DuoFightSimulateBounce(float gravity)
	{
		Vector2 simulatedPosition = NPC.Center;
		Vector2 simulatedVelocity = NPC.velocity;
		Vector2 simulatedTargetPosition = Target.Center;
		Vector2 simulatedTargetVelocity = Target.velocity;
		float bounceDuration = 0f;

		simulatedTargetVelocity.X = Math.Clamp(simulatedTargetVelocity.X, -Target.maxRunSpeed, Target.maxRunSpeed);
		simulatedTargetVelocity.Y *= 0;

		for (int i = 0; i < 120; i++)
		{
			bool onTheFloor = SimOnTopOfTiles(simulatedPosition) && simulatedVelocity.Y >= 0;
			float targetDistanceX = Math.Abs(simulatedTargetPosition.X - simulatedPosition.X);
			if (targetDistanceX < 300)
			{
				float extraAllowedHeight = Utils.GetLerpValue(40f, 300f, targetDistanceX, true);
				onTheFloor &= simulatedPosition.Y >= simulatedTargetPosition.Y - 16 - extraAllowedHeight * 200;
			}

			if (onTheFloor)
				break;

			simulatedVelocity.Y += gravity;
			simulatedPosition += simulatedVelocity;
			simulatedTargetPosition += simulatedTargetVelocity;
			bounceDuration++;
		}

		return bounceDuration;

		bool SimOnTopOfTiles(Vector2 simPosition)
		{
			Vector2 cachedPosition = NPC.Center;
			NPC.Center = simPosition;
			bool value = OnTopOfTiles;
			NPC.Center = cachedPosition;
			return value;
		}
	}
	#endregion

	#region Death Anim
	public bool DuoFightDeathIsHappening => (bool)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "inDeathAnim", scourgeFightManager);

	public void DuoFightSpawnDeathGores(Vector2 impartedVelocity)
	{
		if (Main.dedServ)
			return;

		impartedVelocity *= 0.1f;
		Rectangle area = new((int)NPC.Center.X - 50, (int)NPC.Center.Y - 30, 100, 60);

		for (int i = 1; i < 3; i++)
			Gore.NewGoreDirect(NPC.GetSource_Death(), area.TopLeft(), impartedVelocity * 1.5f + Vector2.UnitX * (i == 1 ? -1 : 1) * Main.rand.NextFloat(4f, 10f), Mod.Find<ModGore>("ScarabeusDuo" + i.ToString()).Type, 1f);

		for (int i = 0; i < 3; i++)
		{
			var gore = Gore.NewGoreDirect(NPC.GetSource_Death(), area.Center(), -NPC.velocity * 2.5f + Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f), ModContent.GoreType<ScarabeusGuts>());
			gore.position -= new Vector2(gore.Width, gore.Height) / 2;
			gore.velocity += impartedVelocity;
		}

		for (int i = 0; i < 10; i++)
		{
			Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2);

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f) + impartedVelocity, Color.DarkOrange * 0.4f, Color.Yellow * 0.2f, Main.rand.NextFloat(0.2f, 0.3f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 120), false)
			{
				Pixellate = true,
				DissolveAmount = 1,
				Intensity = 0.9f,
				PixelDivisor = 3,
			});

			Dust.NewDustPerfect(pos, ModContent.DustType<ScarabeusBlood2>(), Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f) + impartedVelocity, 0, default, Main.rand.NextFloat(1f, 2f));
		}

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid"), NPC.Center);
	}

	public float DuoFightDeathAnim(ref bool retarget)
	{
		retarget = false;
		NPC.Opacity = 1f;
		NPC.noGravity = true;

		switch (ExtraMemory)
		{
			//Held in scourge's jaws
			case 0:
				if (scourgeFightManager == null)
				{
					if (Main.netMode != NetmodeID.MultiplayerClient)
						NPC.StrikeInstantKill();
					return 0f;
				}

				bool burntOut = (float)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "deathAnimBurnProgress", scourgeFightManager) >= 1;

				//Ball, but with a shattered elytra
				SetFrame(new Point(0, burntOut ? 7 : 6), PhaseTwoProfile);

				Vector3 positionInfo = (Vector3)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "getPositionInScourgeJaws", scourgeFightManager);
				NPC.Center = new Vector2(positionInfo.X, positionInfo.Y);
				NPC.rotation = positionInfo.Z;
				NPC.behindTiles = true;
				break;

			//Falling down to the ground
			case 1:
				SetFrame(new Point(0, 7), PhaseTwoProfile);
				NPC.realLife = -1;
				NPC.dontTakeDamage = true;
				NPC.behindTiles = true;
				NPC.velocity.Y += 0.3f;
				if (OnTopOfTiles)
				{
					NPC.life = 0;
					NPC.checkDead();
					NPC.HitEffect();
					NPC.active = false;
				}

				break;
		}

		return 1f;
	}
	#endregion

	public override void ResetEffects()
	{
		//Failsafe
		if (FightingDScourge && NPC.life <= 0)
			NPC.life = 1;
	}

	#region Visuals
	public static void DuoFightSpawnRumbleVFX(Rectangle rumbleArea, bool doTileWave)
	{
		if (Main.dedServ)
			return;

		Vector2 rumbleCenter = rumbleArea.Center.ToVector2();
		float rumbleWidth = rumbleArea.Width;

		int maxParticles = rumbleWidth < 150 ? Main.rand.Next(4) : (int)(rumbleWidth / 16);

		for (int i = 0; i < maxParticles; i++)
		{
			Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);
			Color[] colors = GetTilePalette(FindGroundFromPosition(rumbleCenter));

			ParticleHandler.SpawnParticle(new SmokeCloud(Main.rand.NextVector2FromRectangle(rumbleArea), particleVel, colors[0], Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
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
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(rumbleArea), DustID.Sand, new(0, -4), 0, default, Main.rand.NextFloat(0.7f, 1.2f));

		if (doTileWave)
			StaticBouncingTileWave(rumbleCenter, rumbleWidth < 150 ? 5 : (int)(rumbleWidth / 18), Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-rumbleArea.Width / 4, rumbleArea.Width / 4) * Vector2.UnitX);
	}

	public void DrawElectricOutline(SpriteBatch spriteBatch, Effect electroShader, Texture2D texture, Vector2 position, Vector2 origin, Vector2 scale, SpriteEffects effects)
	{
		float electricOpacity = (float)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "scarabElectricShaderOpacity", scourgeFightManager);
		if (electricOpacity <= 0f)
			return;

		//When electrified, draw an outline behind scarab
		float prevOpacity = electroShader.Parameters["uOpacity"].GetValueSingle();
		float glowValue = electroShader.Parameters["glowStrenght"].GetValueSingle();

		//Set bright colors for the shader 
		electroShader.Parameters["uOpacity"]?.SetValue(1f);
		electroShader.Parameters["glowStrenght"]?.SetValue(324f);
		foreach (EffectPass pass in electroShader.CurrentTechnique.Passes)
			pass.Apply();

		for (int i = 0; i < 4; i++)
			spriteBatch.Draw(texture, position + Vector2.UnitX.RotatedBy(i / 4f * MathHelper.TwoPi) * 2f, NPC.frame, Color.White * electricOpacity, NPC.rotation, origin, scale, effects, 0);

		//Reset shader params
		electroShader.Parameters["uOpacity"]?.SetValue(prevOpacity);
		electroShader.Parameters["glowStrenght"]?.SetValue(glowValue);
		foreach (EffectPass pass in electroShader.CurrentTechnique.Passes)
			pass.Apply();
	}

	private static void StaticBouncingTileWave(Vector2 rumblePos, int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		if (Main.dedServ)
			return;

		for (int j = -1; j <= 1; j += 2)
		{
			for (float i = 0; i < numTiles; i++)
			{
				float height = MathHelper.Lerp(maxHeight, 0, EaseFunction.EaseQuadIn.Ease(i / numTiles));
				int delay = (int)MathHelper.Lerp(0, totalTime / 2, (i + 1) / numTiles);
				ParticleHandler.SpawnQueuedParticle(new MovingBlockParticle(FindGroundFromPosition(rumblePos + (offset ?? Vector2.Zero) + j * Vector2.UnitX * 16 * (i + 1)), totalTime / 2, height), delay);
			}
		}

		ParticleHandler.SpawnParticle(new MovingBlockParticle(FindGroundFromPosition(rumblePos + (offset ?? Vector2.Zero)), totalTime / 2, maxHeight));
	}

	public override bool PreHoverInteract(bool mouseIntersects)
	{
		if (mouseIntersects && !Main.LocalPlayer.mouseInterface && FightingDScourge)
		{
			Main.LocalPlayer.cursorItemIconEnabled = false;
			string text = DuoFightHoverText.Format(NPC.GivenOrTypeName, desertScourge.GivenOrTypeName);
			Main.instance.MouseTextHackZoom(text);
			Main.mouseText = true;
			return false;
		}

		return true;
	}
	#endregion
}