using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.Items.HuntingRifle;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Savanna.NPCs.ZombieVariants;

public class SafariZombie : ReplaceNPC
{
	private float frameCounter;

	public override int[] TypesToReplace => [NPCID.Zombie, NPCID.PincushionZombie, NPCID.SlimedZombie, NPCID.SwampZombie];

	public override void StaticDefaults()
	{
		Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Zombie];
		NPCID.Sets.Zombies[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Skeleton;

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Undead);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Humanoid);
	}

	public override void SetDefaults()
	{
		NPC.width = 28;
		NPC.height = 42;
		NPC.damage = 12;
		NPC.defense = 7;
		NPC.lifeMax = 46;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath2;
		NPC.value = 50f;
		NPC.knockBackResist = .45f;
		NPC.aiStyle = 3;
		AIType = NPCID.Zombie;
		AnimationType = NPCID.Zombie;
		Banner = Item.NPCtoBanner(NPCID.Zombie);
		BannerItem = Item.BannerToItem(Banner);
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "NightTime");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 20; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
			for (int i = 1; i < 4; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("SafariZombie" + i).Type, 1f);
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy)
		{
			frameCounter += .1f;
			frameCounter %= Main.npcFrameCount[Type];
			NPC.frame.Y = frameHeight * (int)frameCounter;
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		var rule = ItemDropRule.Common(ModContent.ItemType<HuntingRifle>(), 100);
		rule.OnSuccess(ItemDropRule.Common(ItemID.MusketBall, 1, 25, 45), hideLootReport: true);
		npcLoot.Add(rule);

		npcLoot.AddCommon(ItemID.Shackle, 50);
		npcLoot.AddCommon(ItemID.ZombieArm, 250);
		npcLoot.AddOneFromOptions(75, ModContent.ItemType<Items.Vanity.SafariHat>(), ModContent.ItemType<Items.Vanity.SafariVest>(), ModContent.ItemType<Items.Vanity.SafariShorts>());
	}

	public override bool CanSpawn(Player player) => player.InModBiome<SavannaBiome>();
}