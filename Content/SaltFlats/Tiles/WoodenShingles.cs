using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class WoodenShingles : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(RecipeGroupID.Wood).AddTile(TileID.Sawmill).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		this.Merge(ModContent.TileType<BrownShingles>());
		AddMapEntry(new Color(60, 45, 40));
		DustType = DustID.WoodFurniture;
	}
}