using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.SaltFlats.Tiles;

namespace SpiritReforged.Content.SaltFlats.Walls;

public class SaltPanelWall : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		int block = AutoContent.ItemType<SaltPanel>();
		int wall = AutoContent.ItemType<SaltPanelWall>();

		item.CreateRecipe(4).AddIngredient(block).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(block).AddIngredient(wall, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;

		AddMapEntry(new Color(100, 90, 90));
		DustType = DustID.Pearlsand;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}