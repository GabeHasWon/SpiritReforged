using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.DustStorm;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Savanna.NPCs.SandSlime;

public class SavannaSandSlime : ModNPC
{
	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 3;
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.ShimmerSlime;

		MoRHelper.AddElement(NPC, MoRHelper.Earth);
		MoRHelper.AddElement(NPC, MoRHelper.Water);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Slime);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Hot);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.SandSlime);
		NPC.lifeMax = 28;

		AIType = NPCID.SandSlime;
		AnimationType = NPCID.SandSlime;
		Banner = Item.NPCtoBanner(NPCID.SandSlime);
		BannerItem = Item.BannerToItem(Banner);
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Sandstorm");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 20; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sand, 2.5f * hit.HitDirection, -2.5f, 0, default, 0.78f);
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Gel, 1, 1, 3);
		npcLoot.AddCommon(ItemID.SlimeStaff, 10000);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		var player = spawnInfo.Player;
		return player.InModBiome<SavannaBiome>() && !spawnInfo.PlayerInTown && spawnInfo.SpawnTileType == ModContent.TileType<SavannaGrass>()
			&& player.GetModPlayer<DustStormPlayer>().ZoneDustStorm ? 0.3f : 0;
	}
}