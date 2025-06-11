using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Walls;

public class DrywoodFence : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<Drywood>())
			.AddTile(TileID.WorkBenches).Register();

		//Allow fence items to be crafted back into base materials
		Recipe.Create(AutoContent.ItemType<Drywood>()).AddIngredient(item.Type, 4)
			.AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		WallID.Sets.AllowsPlantsToGrow[Type] = true;
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.WoodFurniture;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}