using ReLogic.Utilities;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Desert.ScarabBoss.Gores;
using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Forest.Relics;
using SpiritReforged.Content.Forest.Trophies;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255,255,255", false)]
public partial class Scarabeus : ModNPC
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
	public static int STAT_DIG_EMERGE_CONTACT_DAMAGE = 26;
	public static int STAT_HORN_SWIPE_CONTACT_DAMAGE = 34;
	public static int STAT_GROUNDPOUND_CONTACT_DAMAGE = 34;
	public static int STAT_ROLL_CONTACT_DAMAGE = 20;
	public static int STAT_SLAM_CONTACT_DAMAGE = 38;
	public static int STAT_FLYDASH_CONTACT_DAMAGE = 38;
	public static float STAT_CONTACT_DAMAGE_EXPERT_MULTIPLIER = 2f;
	public static float STAT_CONTACT_DAMAGE_MASTER_MULTIPLIER = 3f;

	//Projectile damage
	public static int STAT_GROUNDPOUND_SHOCKWAVE_DAMAGE = 30;
	public static int STAT_SLAM_SHOCKWAVE_DAMAGE = 38;
	public static int STAT_DIG_EMERGE_DEBRIS_DAMAGE = 20;
	public static int STAT_ANTLION_SWARMER_DAMAGE = 20;
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

	private static VisualProfile PhaseOneProfile;
	private static VisualProfile PhaseTwoProfile;
	private static VisualProfile SimulatedProfile;

	public delegate float ScarabeusAttackDelegate(ref bool retarget);

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
			return Collision.SolidCollision(collisionPosition, collisionWidth, collisionHeight + 8, !IgnorePlatforms);
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

		SwoopDash,
		Swarm,
		MaxValue
	}

	public bool IsIdling
	{
		get
		{
			AIState currentState = CurrentState;
			return currentState is AIState.IdleTowardsPlayer or AIState.IdleAwayFromPlayer or AIState.IdleBackAwayFast;
		}
	}

	private ScarabeusAttackDelegate[] _stateAI;

	public override void Load()
	{
		PhaseTwoHeadSlot = Mod.AddBossHeadTexture(BossHeadTexture + "2");
		NPCEvents.ModifyCollisionParameters += ShrinkTileHitbox;
		NPCEvents.ModifyJourneyStrengthScaling += NoJourneyScaling;
		ScarabHeatHazeShaderData.Load();
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 17; //The highest frame count
		NPCID.Sets.TrailCacheLength[Type] = 8;
		NPCID.Sets.TrailingMode[Type] = 3;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{
			Position = new Vector2(8f, 12f),
			PortraitPositionXOverride = 0f
		});

		PhaseOneProfile = new(TextureAssets.Npc[Type], DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), [9, 8, 16, 8, 8, 8, 6, 17]);
		PhaseTwoProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), [3, 6, 4, 5, 13, 25], DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Glow", false));
		SimulatedProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSimulated", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), Enumerable.Repeat(4, 11).ToArray(), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSimulated_Glow", false));

		Phase1Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
		Phase2Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus2");
	}

	public override void SetDefaults()
	{
		//Cinematic bits
		_stateAI = new ScarabeusAttackDelegate[(int)AIState.MaxValue];
		_stateAI[(int)AIState.SpawnAnim] = SpawnAnimation;
		_stateAI[(int)AIState.Roar] = Roar;
		_stateAI[(int)AIState.Charmed] = CharmedIdle;
		_stateAI[(int)AIState.Dance] = DanceIdle;
		_stateAI[(int)AIState.PhaseTransitionAnim] = TransitionAnimation;
		_stateAI[(int)AIState.Despawn] = DigAttack;
		_stateAI[(int)AIState.DeathAnim] = DeathAnimation;
		//Idle variants
		_stateAI[(int)AIState.IdleTowardsPlayer] = IdleBetweenAttacks;
		_stateAI[(int)AIState.IdleAwayFromPlayer] = IdleBetweenAttacks;
		_stateAI[(int)AIState.IdleBackAwayFast] = IdleBetweenAttacks;
		//P1 Attacks
		_stateAI[(int)AIState.GroundPound] = GroundPoundAttack;
		_stateAI[(int)AIState.Shockwave] = ShockwaveAttack;
		_stateAI[(int)AIState.Dig] = DigAttack;
		_stateAI[(int)AIState.Roll] = RollAttack;
		//P2 attacks
		_stateAI[(int)AIState.SwoopDash] = SwoopDashAttack;
		_stateAI[(int)AIState.Swarm] = SwarmAttack;

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

		Music = MusicID.Eerie;
	}

	//No journey scaling cuz we aleady scale stuff
	private void NoJourneyScaling(NPC npc, ref float strength)
	{
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

	public override bool CheckActive() => Target.active && !Target.dead;

	public override bool CheckDead()
	{
		if (CurrentState != AIState.DeathAnim)
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

		if (!phaseTwo && NPC.life < NPC.lifeMax * PHASE_2_HEALTH_THRESHOLD && IsIdling)
		{
			ChangeState(AIState.PhaseTransitionAnim);
			NPC.Opacity = 1f;
			phaseTwo = true;
		}

		if (phaseTwo && Music == Phase1Music)
			Music = Phase2Music;

		bool retarget = !IsIdling;
		float counterTickMultiplier = _stateAI[(int)CurrentState](ref retarget);

		//Retarget late if we're attacking and we need to retarget
		if (retarget)
			NPC.TargetClosest(false);
		Counter += counterTickMultiplier;

		HandleDespawn();
		SetContactDamage();
		ManageSandstormffects();
		ScarabHeatHazeShaderData.HeatHazeTargetOpacity = Utils.GetLerpValue(1f, PHASE_2_HEALTH_THRESHOLD, (NPC.life / (float)NPC.lifeMax), true);
	}

	public void SetContactDamage()
	{
		if (CurrentState == AIState.Dig)
			NPC.damage = currentFrame.X == 2 ? STAT_HORN_SWIPE_CONTACT_DAMAGE : STAT_DIG_EMERGE_CONTACT_DAMAGE;
		else if (CurrentState == AIState.Roll)
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
		if (!NPC.HasPlayerTarget || !Target.ZoneDesert || Target.DistanceSQ(NPC.Center) > 1000 * 1000)
		{
			if (++despawnTimer >= 60 * 20 && IsIdling)
				ChangeState(AIState.Despawn);
		}
		else
		{
			despawnTimer = 0;
		}
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

		else if (CurrentState == AIState.Roll)
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
		{
			Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ichor, hit.HitDirection, -1f, 0, default, 1f);
			d.noLight = true;
			d.noGravity = true;
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
				Gore.NewGoreDirect(NPC.GetSource_Death(), area.TopLeft(), NPC.velocity, Mod.Find<ModGore>("Scarabeus" + i.ToString()).Type, 1f);

			for (int i = 0; i < 8; i++)
			{
				var gore = Gore.NewGoreDirect(NPC.GetSource_Death(), area.Center(), Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 6f), ModContent.GoreType<ScarabeusGuts>());
				gore.position -= new Vector2(gore.Width, gore.Height) / 2;
			}

			for (int i = 0; i < 30; i++)
				Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;

			for (int j = 0; j < 50; j++)
			{
				var dust = Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, 1f);
				dust.velocity *= 5f;
				dust.noGravity = true;

				Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, 0.82f).velocity *= 2f;

				ParticleHandler.SpawnParticle(new GlowParticle(
							NPC.Center + Main.rand.NextVector2Circular(NPC.width * 0.66f, NPC.height * 0.66f),
							Main.rand.NextVector2Circular(10f, 10f),
							Color.Orange,
							Main.rand.NextFloat(0.7f, 1f),
							60,
							1,
							DecelerateAction
						));
			}

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid"), NPC.Center);

			static void DecelerateAction(Particle p) => p.Velocity *= 0.925f;

			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2CircularEdge(1f, 1f), 5, 3, 45));
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

		notExpertRule.OnSuccess(ItemDropRule.OneFromOptions(1, ModContent.ItemType<AdornedBow>(), ModContent.ItemType<SunStaff>(), ModContent.ItemType<RoyalKhopesh>()/*, ModContent.ItemType<LocustCrook>()*/));
		notExpertRule.OnSuccess(ItemDropRule.FewFromOptions(2, 1, ModContent.ItemType<BedouinCowl>(), ModContent.ItemType<BedouinBreastplate>(), ModContent.ItemType<BedouinLeggings>()));
		notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ScarabRadio>(), 5));

		npcLoot.Add(notExpertRule);
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabTrophy>(), 6));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabMask>(), 7));
		npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<BagOScarabs>()));

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

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => (NPC.Opacity == 0) ? false : null;

	public override void BossHeadSlot(ref int index)
	{
		if (phaseTwo)
		{
			int slot = PhaseTwoHeadSlot;
			if (slot != -1)
				index = slot;
		}
	}

	public void ChangeState(AIState state)
	{
		for (int i = 0; i < 3; i++)
			NPC.ai[i] = 0;

		CurrentState = state;
		NPC.netUpdate = true;

		if (!phaseTwo)
			NPC.rotation = 0;
		currentFrame.Y = 0;
	}

	public override bool? CanFallThroughPlatforms() => IgnorePlatforms;

	private bool ShrinkTileHitbox(NPC npc, ref Vector2 collisionTopLeft, ref int collisionWidth, ref int collisionHeight)
	{
		if (npc.type == Type)
		{
			collisionWidth = 70;
			collisionHeight = 40;
			collisionTopLeft = new Vector2(npc.Center.X - collisionWidth / 2, npc.Bottom.Y - collisionHeight);
			return true;
		}

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(phaseTwo);
		writer.Write(Enrage);
		writer.Write(scarabColorIndex);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		phaseTwo = reader.ReadBoolean();
		Enrage = reader.ReadSingle();
		scarabColorIndex = reader.ReadInt32();
	}
}