using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class CrackedSandstone : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(ItemID.Sandstone).AddTile(TileID.HeavyWorkBench).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		this.Merge(TileID.Sandstone, TileID.Sand, ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>());
		AddMapEntry(new Color(174, 74, 48));

		DustType = DustID.DynastyShingle_Red;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand, ModContent.TileType<PaleHive>(), ModContent.TileType<GooeyHive>());
}