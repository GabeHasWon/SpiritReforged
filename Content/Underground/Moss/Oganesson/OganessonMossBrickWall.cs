using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

public class OganessonMossBrickWall : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<OganessonMossBrick>()).AddTile(TileID.WorkBenches).Register();

		//Allow wall items to be crafted back into base materials
		Recipe.Create(AutoContent.ItemType<OganessonMossBrick>()).AddIngredient(item.Type, 4).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		AddMapEntry(new Color(126, 126, 126));
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.3f, 0.3f, 0.3f);
}