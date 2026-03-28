using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class BrownShingles : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(RecipeGroupID.Wood).AddTile(TileID.Sawmill).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		AddMapEntry(new Color(60, 45, 40));
		DustType = DustID.BrownMoss;
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		const short rowHeight = 90;

		if (j % 2 == 0)
			Main.tile[i, j].TileFrameY += rowHeight;
	}
}