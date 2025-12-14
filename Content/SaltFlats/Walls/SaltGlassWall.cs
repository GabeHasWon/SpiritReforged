using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Content.SaltFlats.Tiles;
using Terraria.Audio;

namespace SpiritReforged.Content.SaltFlats.Walls;

public class SaltGlassWall : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		int block = AutoContent.ItemType<SaltGlass>();
		int wall = AutoContent.ItemType<SaltGlassWall>();

		item.CreateRecipe(4).AddIngredient(block).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(block).AddIngredient(wall, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		WallID.Sets.AllowsPlantsToGrow[Type] = true;

		AddMapEntry(new Color(100, 90, 90));
		DustType = DustID.Glass;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
		{
			SoundEngine.PlaySound(SoundID.Shatter, new Vector2(i, j).ToWorldCoordinates());
			return false;
		}

		return true;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}