using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Walls;

public class DrywoodWall : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<Drywood>())
			.AddTile(TileID.WorkBenches).Register();

		//Allow wall items to be crafted back into base materials
		Recipe.Create(AutoContent.ItemType<Drywood>()).AddIngredient(item.Type, 4)
			.AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.WoodFurniture;
		AddMapEntry(new Color(107, 70, 50));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}