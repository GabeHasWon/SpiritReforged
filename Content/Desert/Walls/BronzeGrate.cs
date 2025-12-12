using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Forest.Walls;

namespace SpiritReforged.Content.Desert.Walls;

public class BronzeGrate : Trellis, IAutoloadUnsafeWall
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>("BronzeGrateUnsafe").Type;

	public override void AddItemRecipes(ModItem item)
	{
		int plating = AutoContent.ItemType<BronzePlating>();
		int type = item.Type;

		item.CreateRecipe(4).AddIngredient(plating).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(plating).AddIngredient(type, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.Copper;

		var entryColor = new Color(170, 80, 50);
		AddMapEntry(entryColor);

		Main.wallLight[UnsafeType] = true;
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);
	}
}