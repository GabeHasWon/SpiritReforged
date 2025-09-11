using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats.Walls;

public class SaltWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		int salt = AutoContent.ItemType<SaltBlockDull>();
		int wall = AutoContent.ItemType<SaltWall>();

		item.CreateRecipe(4).AddIngredient(salt).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(salt).AddIngredient(wall, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.WoodFurniture;

		var entryColor = new Color(100, 90, 90);
		AddMapEntry(entryColor);
		Mod.Find<ModWall>(Name + "Unsafe").AddMapEntry(entryColor); //Set the unsafe wall's map entry
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}