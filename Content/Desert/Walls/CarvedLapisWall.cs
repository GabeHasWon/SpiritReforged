using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Desert.Tiles;

namespace SpiritReforged.Content.Desert.Walls;

public class CarvedLapisWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>("CarvedLapisWallUnsafe").Type;

	public void AddItemRecipes(ModItem item)
	{
		int lapis = AutoContent.ItemType<CarvedLapis>();
		int type = item.Type;

		item.CreateRecipe(4).AddIngredient(lapis).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(lapis).AddIngredient(type, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.Cobalt;

		var entryColor = new Color(14, 33, 97);
		AddMapEntry(entryColor);
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}