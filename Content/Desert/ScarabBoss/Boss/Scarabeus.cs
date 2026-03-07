using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Forest.Relics;
using SpiritReforged.Content.Forest.Trophies;
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

	public int CurrentState
	{
		get => (int)NPC.ai[0];
		set => NPC.ai[0] = value;
	}

	public int Counter
	{
		get => (int)NPC.ai[1];
		set => NPC.ai[1] = value;
	}

	public Player Target => Main.player[NPC.target];
	public bool IgnorePlatforms => NPC.Center.Y < Target.Top.Y - 20;
	public bool Grounded => NPC.velocity.Y == 0; /*NPC.collideY || CollisionChecks.Tiles(NPC.Hitbox, CollisionChecks.OnlySlopes)*/

	/// <summary> Whether the second phase has started. </summary>
	public bool phaseTwo;
	/// <summary> Whether this NPC should deal contact damage. Resets every frame. </summary>
	public bool dealContactDamage = false;

	private Action[] _states;

	public override void Load()
	{
		PhaseTwoHeadSlot = Mod.AddBossHeadTexture(BossHeadTexture + "2");
		NPCEvents.OnPlatformCollision += PlatformCollision;
	}

	private static void PlatformCollision(NPC npc, ref bool fall)
	{
		if (npc.ModNPC is Scarabeus scarabeus)
			fall = scarabeus.IgnorePlatforms;
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

		PhaseOneProfile = new(TextureAssets.Npc[Type], DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), [7, 8, 16, 8, 8, 8, 6, 17, 2]);
		PhaseTwoProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo", false), DrawHelpers.RequestLocal<Scarabeus>("ScarabeusSheen", false), [3, 6, 4, 5, 13, 25]);
	}

	public override void SetDefaults()
	{
		_states = [
			SpawnAnimation,
			Walking,
			Skitter,
			HornSwipe,
			Leap,
			RollDash,
			GroundedSlam,
			Dig,
			BounceGroundPound,
			Transition,
			FlyHover,
			FlyingDash,
			ChainGroundPound,
			LeapDig,
			ScarabSwarm
		];

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
		NPC.TargetClosest(false);
		NPC.behindTiles = false;
		NPC.ShowNameOnHover = NPC.Opacity != 0;

		dealContactDamage = false;
		showTrail = false;

		if (!phaseTwo && NPC.life < NPC.lifeMax / 2)
		{
			ChangeState(Transition);
			NPC.Opacity = 1f;
			phaseTwo = true;
		}

		_states[CurrentState].Invoke();
		Counter++;
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => dealContactDamage;

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

			//for (int i = 1; i <= 7; i++)
			//	Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Scarab" + i.ToString()).Type, 1f);

			Rectangle area = new((int)NPC.Center.X - 50, (int)NPC.Center.Y - 30, 100, 60);

			for (int i = 0; i < 30; i++)
				Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;

			for (int j = 0; j < 50; j++)
			{
				var dust = Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, 1f);
				dust.velocity *= 5f;
				dust.noGravity = true;

				Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, .82f).velocity *= 2f;
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

	public void ChangeState(Action state) => ChangeState(Array.IndexOf(_states, state));

	public void ChangeState(int state)
	{
		for (int i = 0; i < 4; i++)
			NPC.ai[i] = 0;

		CurrentState = state;
		NPC.netUpdate = true;

		NPC.rotation = 0;
		currentFrame.Y = 0;
	}
}