using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Desert.Tiles;

namespace SpiritReforged.Content.Desert.Walls;

public class BronzePlatingWall : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		int plating = AutoContent.ItemType<BronzePlating>();
		int type = item.Type;

		item.CreateRecipe(4).AddIngredient(plating).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(plating).AddIngredient(type, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.Copper;

		AddMapEntry(new Color(170, 80, 50));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}