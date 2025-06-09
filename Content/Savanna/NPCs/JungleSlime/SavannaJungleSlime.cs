using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Savanna.Biome;
using Terraria;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Savanna.NPCs.JungleSlime;

public class SavannaJungleSlime : ModNPC
{
	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 2;
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.ShimmerSlime;

		MoRHelper.AddElement(NPC, MoRHelper.Water);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Slime);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.SandSlime);
		NPC.color = Color.White * .25f;

		AIType = NPCID.JungleSlime;
		AnimationType = NPCID.BlueSlime;
		Banner = Item.NPCtoBanner(NPCID.JungleSlime);
		BannerItem = Item.BannerToItem(Banner);
		NPC.alpha = 60;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void AI() => NPC.spriteDirection = -NPC.direction;

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Jungle");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 10; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.JungleGrass, 2.5f * hit.HitDirection, -2.5f, 0, Color.Yellow * .25f, 0.78f);
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Gel, 1, 1, 3);
		npcLoot.AddCommon(ItemID.SlimeStaff, 10000);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
		=> spawnInfo.Player.InModBiome<SavannaBiome>() && !spawnInfo.PlayerInTown && spawnInfo.SpawnTileType == TileID.JungleGrass && Main.dayTime ? 0.1f : 0;
}