using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Savanna.Tiles;

internal class LivingBaobabLeaf : ModTile
{
	public override void SetStaticDefaults()
	{
		TileID.Sets.IsSkippedForNPCSpawningGroundTypeCheck[Type] = true;

		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		AddMapEntry(new Color(140, 156, 55));
		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;
	}

	public override void RandomUpdate(int i, int j)
	{
		const int cap = 10;

		if (Main.rand.NextBool(50) && WorldGen.CountNearBlocksTypes(i, j, Placer.HerbRadius, cap, ModContent.TileType<HangingBaobabFruit>()) < cap) //Randomly grow hanging baobab fruit
			HangingBaobabFruit.GrowVine(i, ++j);
	}
}
