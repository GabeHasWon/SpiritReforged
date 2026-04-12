using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Ziggurat.Tiles;

namespace SpiritReforged.Content.Ziggurat.Walls;

public class PolishedSandstoneWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>("PolishedSandstoneWallUnsafe").Type;

	public void AddItemRecipes(ModItem item)
	{
		int brick = ItemID.Sandstone;
		int type = item.Type;

		item.CreateRecipe(4).AddIngredient(brick).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(brick).AddIngredient(type, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.DynastyShingle_Red;

		var entryColor = new Color(154, 90, 28);
		AddMapEntry(entryColor);
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}