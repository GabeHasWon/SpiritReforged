using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon.Abstract;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ocean.NPCs.ZombieVariants;

public class TridentZombie : ReplaceNPC
{
	float frameCounter;

	public override int[] TypesToReplace => [NPCID.ArmedZombie, NPCID.ArmedZombieCenx];

	public override void StaticDefaults()
	{
		Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.ArmedZombie];
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.BoneThrowingSkeleton;
		NPCID.Sets.Zombies[Type] = true;

		MoRHelper.AddElement(NPC, MoRHelper.Water);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Undead);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Humanoid);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Wet);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Armed);
	}

	public override void SetDefaults()
	{
		NPC.width = 36;
		NPC.height = 42;
		NPC.damage = 30;
		NPC.defense = 5;
		NPC.lifeMax = 80;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath2;
		NPC.value = 60f;
		NPC.knockBackResist = .55f;
		NPC.aiStyle = 3;
		AIType = NPCID.ArmedZombie;
		AnimationType = NPCID.ArmedZombie;
		Banner = Item.NPCtoBanner(NPCID.Zombie);
		BannerItem = Item.BannerToItem(Banner);
	}
	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "NightTime Ocean Moon");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 20; k++)
		{
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.Green, .54f);
		}

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("KelpZombie1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("KelpZombie2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("KelpZombie3").Type, 1f);
		}
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
		npcLoot.AddCommon(ItemID.Shackle, 50);
		npcLoot.AddCommon(ItemID.ZombieArm, 250);
		npcLoot.AddCommon(ModContent.ItemType<Items.Kelp>(), 10, 1, 2);
		npcLoot.AddCommon(ItemID.Trident, 120);
	}

	public override bool CanSpawn(Player player) => player.ZoneBeach;
}