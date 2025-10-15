using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Desert.Tiles;

namespace SpiritReforged.Content.Desert.Walls;

public class PolishedSandstoneWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>("PolishedSandstoneWallUnsafe").Type;

	public void AddItemRecipes(ModItem item)
	{
		int pillar = AutoContent.ItemType<RuinedSandstonePillar>();
		int type = item.Type;

		item.CreateRecipe(4).AddIngredient(pillar).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(pillar).AddIngredient(type, 4).AddTile(TileID.WorkBenches).Register();
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