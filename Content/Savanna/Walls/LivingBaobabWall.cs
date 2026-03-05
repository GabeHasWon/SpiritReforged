using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Walls;

public class LivingBaobabWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		int drywood = AutoContent.ItemType<Drywood>();
		int livingBaobabWall = AutoContent.ItemType<LivingBaobabWall>();

		item.CreateRecipe(4).AddIngredient(drywood).AddTile(TileID.LivingLoom).Register();
		Recipe.Create(drywood).AddIngredient(livingBaobabWall, 4).AddTile(TileID.LivingLoom).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.WoodFurniture;

		var entryColor = new Color(107, 70, 50);
		AddMapEntry(entryColor);
		Mod.Find<ModWall>(Name + "Unsafe").AddMapEntry(entryColor); //Set the unsafe wall's map entry
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}