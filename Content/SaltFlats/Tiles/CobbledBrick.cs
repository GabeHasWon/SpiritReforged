using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class CobbledBrick : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(1).AddIngredient(ItemID.GrayBrick).AddTile(TileID.HeavyWorkBench).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		Main.tileMerge[ModContent.TileType<SaltBlockDull>()][Type] = true;
		Main.tileMerge[Type][ModContent.TileType<WoodenShingles>()] = true;
		Main.tileMerge[Type][ModContent.TileType<BrownShingles>()] = true;

		TileID.Sets.ChecksForMerge[Type] = true;

		this.Merge(TileID.Stone, TileID.Dirt);
		AddMapEntry(new Color(140, 140, 140));
		DustType = DustID.Stone;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, ModContent.TileType<SaltBlockDull>(), ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}