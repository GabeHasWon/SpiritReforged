using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon.Abstract;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ocean.NPCs.ZombieVariants;

public class SailorZombie : ReplaceNPC
{
	public override int[] TypesToReplace => [NPCID.Zombie, NPCID.BaldZombie,
		NPCID.PincushionZombie, NPCID.SwampZombie, NPCID.TwiggyZombie];

	public override void StaticDefaults()
	{
		Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Zombie];
		NPCID.Sets.Zombies[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.Skeleton;

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Undead);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Humanoid);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Wet);
	}

	public override void SetDefaults()
	{
		NPC.width = 28;
		NPC.height = 42;
		NPC.damage = 14;
		NPC.defense = 6;
		NPC.lifeMax = 45;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath2;
		NPC.value = 50f;
		NPC.knockBackResist = .45f;
		NPC.aiStyle = 3;
		AIType = NPCID.Zombie;
		AnimationType = NPCID.Zombie;
		Banner = Item.NPCtoBanner(NPCID.Zombie);
		BannerItem = Item.BannerToItem(Banner);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "NightTime Ocean Moon");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 20; k++)
		{
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, default, .54f);
		}

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("SailorZombie1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("SailorZombie2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("SailorZombie3").Type, 1f);
		}
	}

	float frameCounter;

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
		npcLoot.AddCommon(ItemID.Shackle, 50);
		npcLoot.AddCommon(ItemID.ZombieArm, 250);
		npcLoot.AddCommon(ModContent.ItemType<Items.Kelp>(), 10, 1, 2);
		npcLoot.AddCommon(ModContent.ItemType<Items.Vanity.SailorCap>(), 50);
	}

	public override bool CanSpawn(Player player) => player.ZoneBeach;
}