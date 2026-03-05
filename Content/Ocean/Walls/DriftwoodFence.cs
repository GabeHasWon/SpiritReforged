using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Walls;

public class DriftwoodFence : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<Driftwood>())
			.AddTile(TileID.WorkBenches).Register();

		//Allow fence items to be crafted back into base materials
		Recipe.Create(AutoContent.ItemType<Driftwood>()).AddIngredient(item.Type, 4)
			.AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		WallID.Sets.AllowsPlantsToGrow[Type] = true;
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.BorealWood;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}