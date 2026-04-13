using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;

namespace SpiritReforged.Content.Desert.Tiles;

public class GildedSandstone : ModTile, IAutoloadTileItem
{
	void IAutoloadTileItem.StaticItemDefaults(ModItem item) => item.Item.ResearchUnlockCount = 100;
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(25).AddIngredient(ItemID.Sandstone, 20).AddRecipeGroup("GoldBars").AddTile(TileID.Anvils).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		this.Merge(TileID.Sandstone, TileID.Sand, TileID.HardenedSand);
		AddMapEntry(new Color(198, 124, 78));

		DustType = DustID.Sand;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);
	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand);
}