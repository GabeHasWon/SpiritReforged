using ReLogic.Utilities;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.NPCCommon.Interfaces;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Desert.ScarabBoss.Dusts;
using SpiritReforged.Content.Desert.ScarabBoss.Gores;
using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Desert.ScarabBoss.Items.Crook;
using SpiritReforged.Content.Desert.ScarabBoss.Items.ScarabPet;
using SpiritReforged.Content.Forest.Relics;
using SpiritReforged.Content.Forest.Trophies;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Content.Ziggurat.Tiles;
using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255,255,255", false)]
public partial class Scarabeus : ModNPC, IBossChecklistProvider
{
	#region SFX
	public static readonly SoundStyle GroundPoundFallSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/Falling");
	public static readonly SoundStyle GroundPoundSlamSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/GroundPound");
	public static readonly SoundStyle BounceSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/Bounce", 3) { MaxInstances = 3};
	public static readonly SoundStyle RollStartSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/RollDash");
	public static readonly SoundStyle SlideScreechSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/Slide");
	public static readonly SoundStyle ChitterSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/Chitter", 4) {  MaxInstances = 5};
	public static readonly SoundStyle SmallChitterSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/SmallChitter", 4) { MaxInstances = 5 };
	public static readonly SoundStyle WindLoopSound = new SoundStyle("SpiritReforged/Assets/SFX/Scarabeus/WindLoop") { IsLooped = true, MaxInstances = 0 };
	private SlotId windLoopSFXInstance;
	#endregion

	#region Balance values
	public static int STAT_LIFEMAX_NORMAL = 3900;
	public static int STAT_LIFEMAX_EXPERT = 4700;
	public static int STAT_LIFEMAX_MASTER = 5700;
	public static int STAT_DEFENSE = 10;

	public static float PHASE_2_HEALTH_THRESHOLD = 0.5f;

	//Idle times
	public static float STAT_MAX_IDLE_TIME = 2.3f; //Random range in seconds for how long scarabeus waits between attacks
	public static float STAT_MIN_IDLE_TIME = 1.8f;
	public static float STAT_IDLE_TIME_REDUCTION_EXPERT = 0.3f; //Amount of time substracted from the idle time in expert
	public static float STAT_IDLE_TIME_REDUCTION_MASTER = 0.45f; //Amount of time substracted from the idle time in master (doesn't stack with the expert reduction)
	public static float STAT_IDLE_TIME_HEALTH_PERCENT_MIN_MULTIPLIER_P1 = 0.9f; //Idle time multiplier which smoothly goes from 1 to this value as scarab's HP falls in phase 1 (Starting out at 100% idle time, and lowering to XX% idle time as it approaches half health)
	public static float STAT_IDLE_TIME_HEALTH_PERCENT_MIN_MULTIPLIER_P2 = 0.7f; //Idle time multiplier which smoothly goes from 1 to this value as scarab's HP falls in phase 2 (Starting out at 100% idle time, and lowering to XX% idle time as it approaches zero health)
	public static float STAT_IDLE_TIME_P2_MULTIPLIER = 1.2f; //Idle time multiplier when scarab is in phase 2

	//Contact damage
	public static int STAT_DIG_EMERGE_CONTACT_DAMAGE = 35;
	public static int STAT_HORN_SWIPE_CONTACT_DAMAGE = 34;
	public static int STAT_GROUNDPOUND_CONTACT_DAMAGE = 34;
	public static int STAT_ROLL_CONTACT_DAMAGE = 35;
	public static int STAT_SLAM_CONTACT_DAMAGE = 38;
	public static int STAT_FLYDASH_CONTACT_DAMAGE = 38;
	public static float STAT_CONTACT_DAMAGE_EXPERT_MULTIPLIER = 2f;
	public static float STAT_CONTACT_DAMAGE_MASTER_MULTIPLIER = 3f;

	//Projectile damage
	public static int STAT_GROUNDPOUND_SHOCKWAVE_DAMAGE = 30;
	public static int STAT_SLAM_SHOCKWAVE_DAMAGE = 38;
	public static int STAT_DIG_EMERGE_DEBRIS_DAMAGE = 20;
	public static int STAT_ANTLION_SWARMER_DAMAGE = 23;
	public static int STAT_ANTLION_ONFIRE_DURATION = 120;
	public static float STAT_PROJECTILE_DAMAGE_EXPERT_MULTIPLIER = 1.7f;
	public static float STAT_PROJECTILE_DAMAGE_MASTER_MULTIPLIER = 2.5f;

	public static int GetProjectileDamage(int baseDamage)
	{
		float damageModified = baseDamage / 2;
		if (Main.masterMode)
			damageModified *= STAT_PROJECTILE_DAMAGE_MASTER_MULTIPLIER / 3f;
		else if (Main.expertMode)
			damageModified *= STAT_PROJECTILE_DAMAGE_EXPERT_MULTIPLIER / 2f;

		return (int)(damageModified);
	}
	#endregion

	private static int SpawningMusic = MusicID.QueenSlime;
	private static int Phase1Music;
	private static int Phase2Music;
	private static int PhaseTwoHeadSlot;

	public static VisualProfile TakeoffProfile;
	private static VisualProfile PhaseOneProfile;
	private static VisualProfile PhaseTwoProfile;	
	private static VisualProfile SimulatedProfile;
	private static VisualProfile BallProfile;
	private static VisualProfile DeadProfile;

	public delegate float ScarabeusAttackDelegate(Scarabeus self, ref bool retarget);

	public AIState CurrentState
	{
		get => (AIState)NPC.ai[0];
		set => NPC.ai[0] = (int)value;
	}

	public ref float Counter => ref NPC.ai[1];
	public ref float ExtraMemory => ref NPC.ai[2];

	public AIState LastAttack
	{
		get => (AIState)NPC.ai[3];
		set => NPC.ai[3] = (int)value;
	}

	private static float DifficultyScale => Main.masterMode ? 3 : Main.expertMode ? 2 : 0;

	/// <summary> The player currently targeted by this NPC. </summary>qq
	public Player Target => Main.player[NPC.target];

	/// <summary> Whether this NPC should ignore platform collision. </summary>
	public bool IgnorePlatforms => NPC.Bottom.Y < Target.Top.Y;

	public bool OnTopOfTiles
	{
		get
		{
			Vector2 collisionPosition = NPC.position;
			int collisionWidth = NPC.width;
			int collisionHeight = NPC.height;
			ShrinkTileHitbox(NPC, ref collisionPosition, ref collisionWidth, ref collisionHeight);
			return CollisionHelper.SolidCollision(collisionPosition, collisionWidth, collisionHeight + 8, !IgnorePlatforms);
		}
	}

	public bool IsIdling
	{
		get
		{
			AIState currentState = CurrentState;
			return currentState is AIState.IdleTowardsPlayer or AIState.IdleAwayFromPlayer or AIState.IdleBackAwayFast or AIState.DuoFightIdleStandStill;
		}
	}

	/// <summary> Whether the second phase has started. </summary>
	public bool phaseTwo;

	/// <summary> Annoyance value that increases when the player breaks line of sight with scarabeus. Makes it close in towards the player faster. </summary>
	public float Enrage
	{
		get => _enrage;
		set => _enrage = Math.Clamp(value, 0, 1);
	}

	private float _enrage;

	/// <summary> Whether this NPC should deal contact damage. Resets every frame. </summary>
	public bool dealContactDamage = false;

	/// <summary> Tracks when Scarabeus should despawn. </summary>
	public int despawnTimer;

	public int _shakeTimer;

	public enum AIState
	{
		SpawnAnim,
		Roar,
		Charmed,
		Dance,
		PhaseTransitionAnim,
		Despawn,
		DeathAnim,

		IdleTowardsPlayer,
		IdleAwayFromPlayer,
		IdleBackAwayFast,

		//P1 Attacks
		GroundPound,
		Shockwave,
		Dig,
		Roll,

		//P2 Attacks
		SwoopDash,
		Swarm,

		//Duo Fight Attacks
		DuoFightSpawnAnim,
		DuoFightSpawnAnimFallback,
		DuoFightIdleStandStill,
		DuoFightGrabbedByScourge,
		DuoFightTakeoff,
		DuoFightEaten,
		DuoFightGunkRoll,
		DuoFightFollowLeader,
		DuoFightFlyBackToTheFloor,
		DuoFightDeathSwarm,
		DuoFightDeathAnim,

		MaxValue
	}

	private static ScarabeusAttackDelegate[] _stateAI;

	public override void Load()
	{
		PhaseTwoHeadSlot = Mod.AddBossHeadTexture(BossHeadTexture + "2");
		NPCEvents.ModifyCollisionParameters += ShrinkTileHitbox;
		NPCEvents.ModifyJourneyStrengthScaling += NoJourneyScaling;
		ScarabHeatHazeShaderData.Load();
	}

	public override void SetStaticDefaults()
	{
		//Cinematic bits
		_stateAI = new ScarabeusAttackDelegate[(int)AIState.MaxValue];
		_stateAI[(int)AIState.SpawnAnim] = (Scarabeus scarab, ref bool retarget) => scarab.SpawnAnimation(ref retarget);
		_stateAI[(int)AIState.Roar] = (Scarabeus scarab, ref bool retarget) => scarab.Roar(ref retarget);
		_stateAI[(int)AIState.Charmed] = (Scarabeus scarab, ref bool retarget) => scarab.CharmedIdle(ref retarget);
		_stateAI[(int)AIState.Dance] = (Scarabeus scarab, ref bool retarget) => scarab.DanceIdle(ref retarget);
		_stateAI[(int)AIState.PhaseTransitionAnim] = (Scarabeus scarab, ref bool retarget) => scarab.TransitionAnimation(ref retarget);
		_stateAI[(int)AIState.Despawn] = (Scarabeus scarab, ref bool retarget) => scarab.DigAttack(ref retarget);
		_stateAI[(int)AIState.DeathAnim] = (Scarabeus scarab, ref bool retarget) => scarab.DeathAnimation(ref retarget);
		//Idle variants
		_stateAI[(int)AIState.IdleTowardsPlayer] = (Scarabeus scarab, ref bool retarget) => scarab.IdleBetweenAttacks(ref retarget);
		_stateAI[(int)AIState.IdleAwayFromPlayer] = (Scarabeus scarab, ref bool retarget) => scarab.IdleBetweenAttacks(ref retarget);
		_stateAI[(int)AIState.IdleBackAwayFast] = (Scarabeus scarab, ref bool retarget) => scarab.IdleBetweenAttacks(ref retarget);
		//P1 Attacks
		_stateAI[(int)AIState.GroundPound] = (Scarabeus scarab, ref bool retarget) => scarab.GroundPoundAttack(ref retarget);
		_stateAI[(int)AIState.Shockwave] = (Scarabeus scarab, ref bool retarget) => scarab.ShockwaveAttack(ref retarget);
		_stateAI[(int)AIState.Dig] = (Scarabeus scarab, ref bool retarget) => scarab.DigAttack(ref retarget);
		_stateAI[(int)AIState.Roll] = (Scarabeus scarab, ref bool retarget) => scarab.RollAttack(ref retarget);
		//P2 attacks
		_stateAI[(int)AIState.SwoopDash] = (Scarabeus scarab, ref bool retarget) => scarab.SwoopDashAttack(ref retarget);
		_stateAI[(int)AIState.Swarm] = (Scarabeus scarab, ref bool retarget) => scarab.SwarmAttack(ref retarget);

		//Duo fight
		_stateAI[(int)AIState.DuoFightSpawnAnim] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightSpawnAnimation(ref retarget);
		_stateAI[(int)AIState.DuoFightSpawnAnimFallback] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightSpawnFallback(ref retarget);
		_stateAI[(int)AIState.DuoFightIdleStandStill] = (Scarabeus scarab, ref bool retarget) => scarab.IdleBetweenAttacks(ref retarget);
		_stateAI[(int)AIState.DuoFightGrabbedByScourge] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightGrabbedByScourge(ref retarget);
		_stateAI[(int)AIState.DuoFightTakeoff] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightTakeoff(ref retarget);
		_stateAI[(int)AIState.DuoFightEaten] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightGrabbedByScourge(ref retarget);
		_stateAI[(int)AIState.DuoFightGunkRoll] = (Scarabeus scarab, ref bool retarget) => scarab.RollAttack(ref retarget);
		_stateAI[(int)AIState.DuoFightFollowLeader] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightFlyFollowLeader(ref retarget);
		_stateAI[(int)AIState.DuoFightFlyBackToTheFloor] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightFlyDownToGround(ref retarget);
		_stateAI[(int)AIState.DuoFightDeathSwarm] = (Scarabeus scarab, ref bool retarget) => scarab.SwarmAttack(ref retarget);
		_stateAI[(int)AIState.DuoFightDeathAnim] = (Scarabeus scarab, ref bool retarget) => scarab.DuoFightDeathAnim(ref retarget);

		Main.npcFrameCount[Type] = 17; //The highest frame count
		NPCID.Sets.TrailCacheLength[Type] = 8;
		NPCID.Sets.TrailingMode[Type] = 3;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{
			Position = new Vector2(8f, 12f),
			PortraitPositionXOverride = 0f
		});

		PhaseOneProfile = new(TextureAssets.Npc[Type], DrawHelpers.RequestLocal<Scarabeus>("Scarabeus_Sheen", false), [9, 8, 16, 8, 8, 8, 6, 17]);
		PhaseTwoProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Sheen", false), [3, 6, 5, 13], DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Glow", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Wings", false));
		SimulatedProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSimulated", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSimulated_Sheen", false), Enumerable.Repeat(4, 11).ToArray(), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSimulated_Glow", false), simulated: true);
		BallProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusBall", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusBall_Sheen", false), [1]);
		TakeoffProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusTakeoff", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusTakeoff_Sheen", false), [25], DrawHelpers.RequestLocal<Scarabeus>("ScarabeusTakeoff_Glow", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusTakeoff_Wings", false));
		DeadProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusDead", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusDead_Sheen", false), Enumerable.Repeat(4, 11).ToArray(), simulated: true);

		Phase1Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
		Phase2Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/ScarabeusP2");
		LoadDuoFight();
	}

	public override void SetDefaults()
	{
		Profile = PhaseOneProfile;

		NPC.width = 90;
		NPC.height = 90;
		NPC.value = 30000;
		NPC.damage = STAT_HORN_SWIPE_CONTACT_DAMAGE;
		NPC.defense = STAT_DEFENSE;
		NPC.lifeMax = STAT_LIFEMAX_NORMAL;
		NPC.aiStyle = -1;
		NPC.boss = true;
		NPC.npcSlots = 15f;
		NPC.HitSound = SoundID.NPCHit31;
		NPC.DeathSound = SoundID.NPCDeath5;
		NPC.dontTakeDamage = true;
		NPC.knockBackResist = 0;
		NPC.BossBar = ModContent.GetInstance<ScarabeusBossBar>();

		if (!NPC.IsABestiaryIconDummy)
			NPC.Opacity = 0;

		int activeScarabs = Main.npc.Count(n => n.active && n.type == Type);
		scarabColorIndex = activeScarabs - 1;

		Music = 0;
	}

	public override void OnSpawn(IEntitySource source) => CheckDuoFightStart(source);

	//No journey scaling cuz we aleady scale stuff
	private void NoJourneyScaling(NPC npc, ref float strength)
	{
		if (npc.type != ModContent.NPCType<Scarabeus>())
			return;

		if (strength < 1)
			strength = MathHelper.Lerp(strength, 1, 0.5f);
		else
			strength = 1;
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		int usedLifemax = Main.masterMode ? STAT_LIFEMAX_MASTER : Main.expertMode ? STAT_LIFEMAX_EXPERT : STAT_LIFEMAX_NORMAL;
		NPC.lifeMax = (int)(usedLifemax * balance);
		NPC.damage = STAT_HORN_SWIPE_CONTACT_DAMAGE;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override bool CheckActive() => !FightingDScourge && Target.active && !Target.dead;

	public override bool CheckDead()
	{
		if (CurrentState != AIState.DeathAnim && CurrentState != AIState.DuoFightDeathAnim)
		{
			Counter = 0;
			NPC.life = 1;
			NPC.dontTakeDamage = true;

			ChangeState(AIState.DeathAnim);
			return false;
		}
		else
		{
			return true;
		}
	}

	public override void AI()
	{
		if (_shakeTimer > 0)
			_shakeTimer--;

		//Retarget early if we dont have a target or if scarabeus is idling
		if (!NPC.HasValidTarget || IsIdling)
			NPC.TargetClosest(false);

		NPC.behindTiles = false;
		NPC.ShowNameOnHover = NPC.Opacity != 0;

		dealContactDamage = false;
		trailOpacity = 0f;
		iridescenceBoost = MathHelper.Lerp(iridescenceBoost, 0f, 0.1f);

		if (!phaseTwo && NPC.life < NPC.lifeMax * PHASE_2_HEALTH_THRESHOLD && IsIdling && !FightingDScourge)
		{
			ChangeState(AIState.PhaseTransitionAnim);
			NPC.Opacity = 1f;
			phaseTwo = true;
		}

		if (phaseTwo && Music == Phase1Music)
			Music = Phase2Music;

		bool retarget = !IsIdling;
		float counterTickMultiplier = _stateAI[(int)CurrentState](this, ref retarget);

		//Retarget late if we're attacking and we need to retarget
		if (retarget)
			NPC.TargetClosest(false);
		Counter += counterTickMultiplier;

		HandleDespawn();
		SetContactDamage();
		ManageSandstormffects();

		if (Profile == SimulatedProfile || Profile == PhaseTwoProfile || Profile == TakeoffProfile)
		{
			float lightStrength = 0.5f;
			if (Profile == PhaseTwoProfile && currentFrame.X == 2)
			{
				lightStrength = 3f;
			}

			if (Profile == TakeoffProfile && currentFrame.Y < 12)
				lightStrength = 0;

			Lighting.AddLight(NPC.Center, 1f * lightStrength, 1f * lightStrength, 0.2f * lightStrength);
		}

		if (Main.dayTime)
			ScarabHeatHazeShaderData.HeatHazeTargetOpacity = Utils.GetLerpValue(1f, PHASE_2_HEALTH_THRESHOLD, (NPC.life / (float)NPC.lifeMax), true);
	}

	public void SetContactDamage()
	{
		if (CurrentState == AIState.Dig)
			NPC.damage = currentFrame.X == 2 ? STAT_HORN_SWIPE_CONTACT_DAMAGE : STAT_DIG_EMERGE_CONTACT_DAMAGE;
		else if (CurrentState == AIState.Roll || CurrentState == AIState.DuoFightGunkRoll)
			NPC.damage = STAT_ROLL_CONTACT_DAMAGE;
		else if (CurrentState == AIState.GroundPound)
			NPC.damage = STAT_GROUNDPOUND_CONTACT_DAMAGE;
		else if (CurrentState == AIState.Shockwave)
			NPC.damage = STAT_GROUNDPOUND_SHOCKWAVE_DAMAGE;
		else if (CurrentState == AIState.SwoopDash)
			NPC.damage = STAT_FLYDASH_CONTACT_DAMAGE;

		//Apply master & expert multipliers
		if (Main.masterMode)
			NPC.damage = (int)(NPC.damage * STAT_CONTACT_DAMAGE_EXPERT_MULTIPLIER);
		else if (Main.expertMode)
			NPC.damage = (int)(NPC.damage * STAT_CONTACT_DAMAGE_MASTER_MULTIPLIER);
	}

	public void HandleDespawn()
	{
		if (FightingDScourge)
		{
			despawnTimer = 0;
			return;
		}

		if (!NPC.HasPlayerTarget || !Target.ZoneDesert || !Main.dayTime || Target.DistanceSQ(NPC.Center) > 1000 * 1000)
		{
			if (++despawnTimer >= 60 * 20 && IsIdling)
				ChangeState(AIState.Despawn);
		}
		else
			despawnTimer = 0;
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => dealContactDamage;

	public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
	{
		if (CurrentState == AIState.GroundPound && NPC.velocity.Y >= 0)
		{
			if (NPC.ai[2] < GroundPoundBounceCount)
				npcHitbox.Inflate(8, 15);
			else
				npcHitbox.Inflate(20, 15);
		}

		else if (CurrentState == AIState.Shockwave)
		{
			npcHitbox.Inflate(40, 0);
			npcHitbox.X += NPC.direction * 35;
		}
		//Its the dig state but this is the hitbox for the horn swipe it does at the end specifically!
		else if (CurrentState == AIState.Dig && currentFrame.X == 2)
		{
			npcHitbox.Inflate(10, 10);
			npcHitbox.X += NPC.direction * 45;
		}

		else if (CurrentState == AIState.Roll || CurrentState == AIState.DuoFightGunkRoll)
		{
			//Shave off the top of the hitbox when rolling to make it easier to jump over
			npcHitbox.Y += 20;
		}

		return true;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 9; i++)
			Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, ModContent.DustType<ScarabeusBlood>(), hit.HitDirection, -1f, 0, default, 1.3f).noGravity = true;

		if (Main.rand.NextBool(5))
		{
			for (int i = 0; i < 9; i++)
				Dust.NewDustPerfect(NPC.Center, ModContent.DustType<ScarabeusBlood>(), NPC.DirectionTo(Target.Center).RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, 9f), 50, default, 1.5f).noGravity = false;
		}

		if (NPC.life <= 0 && CurrentState == AIState.DeathAnim)
		{
			//SoundEngine.TryGetActiveSound(wingSoundSlot, out ActiveSound sound);

			//if (sound is not null && sound.IsPlaying)
			//{
			//	sound.Stop();
			//wingSoundSlot = SlotId.Invalid;
			//}

			Rectangle area = new((int)NPC.Center.X - 50, (int)NPC.Center.Y - 30, 100, 60);

			for (int i = 1; i < 12; i++)
				Gore.NewGoreDirect(NPC.GetSource_Death(), area.TopLeft(), -NPC.velocity * 2.5f, Mod.Find<ModGore>("Scarabeus" + i.ToString()).Type, 1f);

			for (int i = 0; i < 12; i++)
			{
				var gore = Gore.NewGoreDirect(NPC.GetSource_Death(), area.Center(), -NPC.velocity * 2.5f + Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 6f), ModContent.GoreType<ScarabeusGuts>());
				gore.position -= new Vector2(gore.Width, gore.Height) / 2;
			}

			Vector2 velocity = -NPC.velocity * 0.7f;

			for (int i = 0; i < 30; i++)
			{
				Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2);		
				
				ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity.RotatedByRandom(1.5f) * Main.rand.NextFloat(2), Color.DarkOrange * 0.4f, Color.Yellow * 0.2f, Main.rand.NextFloat(0.2f, 0.3f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 120), false)
				{
					Pixellate = true,
					DissolveAmount = 1,
					Intensity = 0.9f,
					PixelDivisor = 3,
				});

				ParticleHandler.SpawnParticle(new SmokeCloud(pos, -NPC.velocity.RotatedByRandom(2.5f) * Main.rand.NextFloat(2), Color.DarkOrange * 0.4f, Color.Yellow * 0.2f, Main.rand.NextFloat(0.2f, 0.4f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 120), false)
				{
					Pixellate = true,
					DissolveAmount = 1,
					Intensity = 0.9f,
					PixelDivisor = 3,
				});

				Dust.NewDustPerfect(pos, ModContent.DustType<ScarabeusBlood2>(), velocity.RotatedByRandom(1.65f) * Main.rand.NextFloat(0.8f), 0, default, Main.rand.NextFloat(1f, 2f));

				Dust.NewDustPerfect(pos, ModContent.DustType<ScarabeusBlood2>(), velocity.RotatedByRandom(0.95f) * Main.rand.NextFloat(0.8f), 0, default, Main.rand.NextFloat(1f, 2f));

				Dust.NewDustPerfect(pos, ModContent.DustType<ScarabeusBlood>(), velocity.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.8f), 50 + Main.rand.Next(100), default, 1.6f).noGravity = true;

				Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;
			}

			for (int j = 0; j < 50; j++)
			{
				var dust = Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, 1f);
				dust.velocity *= 5f;
				dust.noGravity = true;

				Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, 0.82f).velocity *= 2f;

				Dust.NewDustPerfect(NPC.Center, ModContent.DustType<ScarabeusBlood>(), -NPC.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(2f), 50, default, 2.5f).noGravity = false;
			}

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid"), NPC.Center);

			ScreenshakeHelper.Shake(NPC.Center, Main.rand.NextVector2CircularEdge(1f, 1f), 20, 4, 45);
		}
	
		if (NPC.life <= 0 && CurrentState == AIState.DuoFightDeathAnim)
		{
			Gore g = Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.Center - Vector2.One * 60, -Vector2.UnitY * 0.4f, Mod.Find<ModGore>(NPC.direction == 1 ? "ScarabeusCharredFlip"  : "ScarabeusCharred").Type, 1f);
			g.behindTiles = true;
			GoreID.Sets.DrawBehind[g.type] = true;
			g.rotation = NPC.rotation;
			g.timeLeft = 100;
		}
	}

	public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (projectile.TryGetOwner(out Player player) && !player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (!player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public void ManageSandstormffects()
	{
		foreach (Player Player in Main.player.Where(p => p.active && !p.dead))
			Player.buffImmune[BuffID.WindPushed] = true;

		if (!phaseTwo || CreativePowerManager.Instance.GetPower<CreativePowers.FreezeWindDirectionAndStrength>().Enabled || CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled)
			return;

		Sandstorm.Happening = true;
		Sandstorm.TimeLeft = 60;
		if (Sandstorm.TimeLeft < 2)
			Sandstorm.TimeLeft = 2;

		//Sandstorm ramps up as the fight progresses
		float intendedSandstormPower = 0.2f + 0.8f * Utils.GetLerpValue(PHASE_2_HEALTH_THRESHOLD, 0.2f, NPC.life / (float)NPC.lifeMax, true);
		float sandstormPower = Math.Max(MathHelper.Lerp(Sandstorm.Severity, intendedSandstormPower, 0.2f), 0.2f);

		Sandstorm.Severity = Math.Max(Sandstorm.Severity, sandstormPower);
		Sandstorm.IntendedSeverity = Math.Max(Sandstorm.IntendedSeverity, sandstormPower);
		Main.windSpeedTarget = 0.8f;
	}

	/*public override bool PreKill()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.WorldData);

		//NPC.PlayDeathSound("ScarabDeathSound");
		return true;
	}*/

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		LeadingConditionRule notExpertRule = new(new Conditions.NotExpert());

		notExpertRule.OnSuccess(ItemDropRule.OneFromOptions(1, ModContent.ItemType<AdornedBow>(), ModContent.ItemType<SunStaff>(), ModContent.ItemType<RoyalKhopesh>(), ModContent.ItemType<LocustCrook>()));
		notExpertRule.OnSuccess(ItemDropRule.FewFromOptions(2, 1, ModContent.ItemType<BedouinCowl>(), ModContent.ItemType<BedouinBreastplate>(), ModContent.ItemType<BedouinLeggings>()));
		notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ScarabRadio>(), 5));
		notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SpaceHeater>(), 8));
		notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<IridescentDye>(), 4, 3, 3));
		notExpertRule.OnSuccess(ItemDropRule.Common(ItemID.ScarabBomb, 1, 8, 12));
		notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BeetleLicense>(), 4));

		npcLoot.Add(notExpertRule);
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabTrophy>(), 6));
		npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<BagOScarabs>()));

		npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<ScarabLightPetItem>()));
		npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<ScarabRelic>()));
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		//Zero knockback because we will do our own custom KB in OnHitPlayer
		if (CurrentState == AIState.Dig)
			modifiers.Knockback *= 0;
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
	{
		//Horn swipe KB
		if (CurrentState == AIState.Dig)
		{
			target.velocity.Y -= 10f;
			target.velocity.X += NPC.direction * 4f;
			target.fallStart = (int)(target.position.Y / 16f);
		}	
	}

	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox) => boundingBox = NPC.Hitbox;

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => (NPC.Opacity == 0) ? false : !FightingDScourge;

	public override void BossHeadSlot(ref int index)
	{
		if (phaseTwo)
		{
			int slot = PhaseTwoHeadSlot;
			if (slot != -1)
				index = slot;
		}

		if (CurrentState == AIState.DuoFightEaten)
			index = -1;
	}

	public void ChangeState(AIState state, bool setIdleTime = false)
	{
		for (int i = 1; i < 3; i++)
			NPC.ai[i] = 0;

		CurrentState = state;
		NPC.netUpdate = true;

		if (!phaseTwo)
			NPC.rotation = 0;

		currentFrame.Y = 0;

		if (setIdleTime) // Pick a time to wait before the next attack
			SetIdleTime(ref ExtraMemory);
	}

	public override bool? CanFallThroughPlatforms() => IgnorePlatforms;

	private bool ShrinkTileHitbox(NPC npc, ref Vector2 collisionTopLeft, ref int collisionWidth, ref int collisionHeight)
	{
		if (npc.type == Type)
		{
			collisionWidth = 70;
			collisionHeight = 40;
			collisionTopLeft = new Vector2(npc.Center.X - collisionWidth / 2, npc.Bottom.Y - collisionHeight);

			//Don't hit the ground as immediately when being a burnt charred corpse
			if (CurrentState == AIState.DuoFightDeathAnim)
				collisionTopLeft.Y -= 30;

			return true;
		}

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(phaseTwo);
		writer.Write(Enrage);
		writer.Write(scarabColorIndex);
		writer.Write((Half)NPC.Opacity);
		SyncDuoFightStuff(writer);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		phaseTwo = reader.ReadBoolean();
		Enrage = reader.ReadSingle();
		scarabColorIndex = reader.ReadInt32();
		NPC.Opacity = (float)reader.ReadHalf();
		SyncDuoFightStuff(reader);
	}

	BossChecklistData IBossChecklistProvider.ChecklistData() => new(2.2f, () => BossFlags.Downed(Type), new LocalizableFunc(this.GetLocalization("SpawnInfo"), null),
	[
		ModContent.ItemType<ScarabMask>(), ModContent.ItemType<ScarabRelic>(), ModContent.ItemType<ScarabTrophy>()
	], [ModContent.GetInstance<ScarabAltar>().AutoItemType()]);

	Action<SpriteBatch, Rectangle, Color> IBossChecklistProvider.PreDrawPortrait => (batch, rectangle, color) =>
	{
		Texture2D tex = ModContent.Request<Texture2D>("SpiritReforged/Content/Desert/ScarabBoss/Boss/Scarabeus_Checklist").Value;
		batch.Draw(tex, rectangle.Center(), null, BossFlags.Downed(Type) ? color : Color.Black, 0f, tex.Size() / 2f, 1f, SpriteEffects.None, 0);
	};
}