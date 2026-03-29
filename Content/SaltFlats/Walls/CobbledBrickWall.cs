using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.SaltFlats.Tiles;

namespace SpiritReforged.Content.SaltFlats.Walls;

public class CobbledBrickWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>(nameof(CobbledBrickWall) + "Unsafe").Type;

	public void AddItemRecipes(ModItem item)
	{
		int brick = AutoContent.ItemType<CobbledBrick>();
		int wall = this.AutoItemType();

		item.CreateRecipe(4).AddIngredient(brick).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(brick).AddIngredient(wall, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;

		Color entryColor = new(100, 90, 90);
		AddMapEntry(entryColor);
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);

		DustType = DustID.Stone;

		this.AutoItem().ResearchUnlockCount = 400;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}