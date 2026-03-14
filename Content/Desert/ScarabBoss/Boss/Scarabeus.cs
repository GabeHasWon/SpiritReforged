using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Desert.ScarabBoss.Gores;
using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Forest.Relics;
using SpiritReforged.Content.Forest.Trophies;
using System.IO;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255,255,255", false)]
public partial class Scarabeus : ModNPC
{
	private static VisualProfile PhaseOneProfile;
	private static VisualProfile PhaseTwoProfile;
	private static int PhaseTwoHeadSlot;

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

	/// <summary> Whether this NPC is in contact with the ground. </summary>
	public bool Grounded => NPC.velocity.Y == 0; /*NPC.collideY || CollisionChecks.Tiles(NPC.Hitbox, CollisionChecks.OnlySlopes)*/

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

	public enum AIState
	{
		SpawnAnim,
		Charmed,
		PhaseTransitionAnim,

		IdleTowardsPlayer,
		IdleAwayFromPlayer,
		IdleBackAwayFast,

		//P1 Attacks
		GroundPound,
		Shockwave,
		Dig,
		Roll,

		FlyingDash,
		Swarm,
		MaxValue
	}

	public bool IsIdling
	{
		get
		{
			AIState currentState = CurrentState;
			return currentState == AIState.IdleTowardsPlayer || currentState == AIState.IdleAwayFromPlayer || currentState == AIState.IdleBackAwayFast;
		}
	}

	private ScarabeusAttackDelegate[] _stateAI;

	public override void Load()
	{
		PhaseTwoHeadSlot = Mod.AddBossHeadTexture(BossHeadTexture + "2");
		NPCEvents.ModifyCollisionParameters += ShrinkTileHitbox; 
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

		PhaseOneProfile = new(TextureAssets.Npc[Type], DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), [8, 8, 16, 8, 8, 8, 6, 17]);
		PhaseTwoProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), [3, 6, 4, 5, 13, 25]);
	}

	public override void SetDefaults()
	{
		//Cinematic bits
		_stateAI = new ScarabeusAttackDelegate[(int)AIState.MaxValue];
		_stateAI[(int)AIState.SpawnAnim] = SpawnAnimation;
		_stateAI[(int)AIState.Charmed] = CharmedIdle;
		_stateAI[(int)AIState.PhaseTransitionAnim] = TransitionAnimation;
		//Idle variants
		_stateAI[(int)AIState.IdleTowardsPlayer]       = IdleBetweenAttacks;
		_stateAI[(int)AIState.IdleAwayFromPlayer] = IdleBetweenAttacks;
		_stateAI[(int)AIState.IdleBackAwayFast] = IdleBetweenAttacks;
		//P1 Attacks
		_stateAI[(int)AIState.GroundPound] = GroundPoundAttack;
		_stateAI[(int)AIState.Shockwave] = ShockwaveAttack;
		_stateAI[(int)AIState.Dig] = DigAttack;
		_stateAI[(int)AIState.Roll] = RollAttack;
		//P2 attacks
		_stateAI[(int)AIState.FlyingDash] = FlyingDashAttack;
		_stateAI[(int)AIState.Swarm] = SwarmAttack;

		Profile = PhaseOneProfile;

		NPC.width = 90;
		NPC.height = 90;
		NPC.value = 30000;
		NPC.damage = 40;
		NPC.defense = 10;
		NPC.lifeMax = 2550;
		NPC.aiStyle = -1;
		NPC.boss = true;
		NPC.npcSlots = 15f;
		NPC.HitSound = SoundID.NPCHit31;
		NPC.DeathSound = SoundID.NPCDeath5;
		NPC.dontTakeDamage = true;
		NPC.knockBackResist = 0;

		if (!NPC.IsABestiaryIconDummy)
			NPC.Opacity = 0;

		Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override bool CheckActive() => Target.active && !Target.dead;

	public override void AI()
	{
		//Retarget early if we dont have a target or if scarabeus is idling
		if (!NPC.HasValidTarget || IsIdling)
			NPC.TargetClosest(false);

		NPC.behindTiles = false;
		NPC.ShowNameOnHover = NPC.Opacity != 0;

		dealContactDamage = false;
		showTrail = false;
		iridescenceBoost = MathHelper.Lerp(iridescenceBoost, 0f, 0.1f);

		if (!phaseTwo && NPC.life < NPC.lifeMax / 2 && IsIdling)
		{
			ChangeState(AIState.PhaseTransitionAnim);
			NPC.Opacity = 1f;
			phaseTwo = true;
		}

		bool retarget = !IsIdling;
		float counterTickMultiplier = _stateAI[(int)CurrentState](ref retarget);

		//Retarget late if we're attacking and we need to retarget
		if (retarget)
			NPC.TargetClosest(false);
		Counter += counterTickMultiplier;

		ScarabHeatHazeShaderData.AnyScarabeusPresent = true;
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => dealContactDamage;

	public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
	{
		if (CurrentState == AIState.GroundPound && NPC.velocity.Y > 0 && NPC.ai[2] < GroundPoundBounceCount)
		{
			npcHitbox.Inflate(15, 15);
		}
		else if (CurrentState == AIState.Shockwave)
		{
			npcHitbox.Inflate(40, 0);
			npcHitbox.X += NPC.direction * 35;
		}
		//Its the dig state but this is the hitbox for the dig attack!
		else if (CurrentState == AIState.Dig)
		{
			npcHitbox.Inflate(-20, 10);
			npcHitbox.X += NPC.direction * 65;
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

		for (int i = 0; i < 5; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, 1f);

		if (NPC.life <= 0)
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
			}
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
		notExpertRule.OnSuccess(ItemDropRule.OneFromOptions(1, ModContent.ItemType<BedouinCowl>(), ModContent.ItemType<BedouinBreastplate>(), ModContent.ItemType<BedouinLeggings>()));
		notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ScarabRadio>(), 5));

		npcLoot.Add(notExpertRule);
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabTrophy>(), 6));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabMask>(), 7));
		npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<BagOScarabs>()));

		npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<ScarabRelic>()));
	}

	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox) => boundingBox = NPC.Hitbox;

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => (NPC.Opacity == 0) ? false : null;

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * (Main.masterMode ? 0.85f : 1.0f) * 0.7143f * balance);
		NPC.damage = (int)(NPC.damage * 0.626f);
	}

	public override void BossHeadSlot(ref int index)
	{
		int slot = PhaseTwoHeadSlot;
		if (phaseTwo && slot != -1)
			index = slot;
	}

	public void ChangeState(AIState state)
	{
		for (int i = 0; i < 3; i++)
			NPC.ai[i] = 0;

		CurrentState = state;
		NPC.netUpdate = true;

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
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		phaseTwo = reader.ReadBoolean();
		Enrage = reader.ReadSingle();
	}
}