using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Ziggurat.Tiles;

namespace SpiritReforged.Content.Ziggurat.Walls;

public class RedSandstoneBrickCrackedWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>(nameof(RedSandstoneBrickCrackedWall) + "Unsafe").Type;

	public void AddItemRecipes(ModItem item)
	{
		int brick = AutoContent.ItemType<RedSandstoneBrickCracked>();
		int type = item.Type;

		item.CreateRecipe(4).AddIngredient(brick).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(brick).AddIngredient(type, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallBlend[Type] = ModContent.WallType<RedSandstoneBrickWall>();
		Main.wallBlend[UnsafeType] = ModContent.WallType<RedSandstoneBrickWall>();

		DustType = DustID.DynastyShingle_Red;

		Color entryColor = new(150, 70, 40);
		AddMapEntry(entryColor);
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}