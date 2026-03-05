using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Items.DrywoodSet;

public class DrywoodSword : ModItem
{
	public override void SetDefaults() => Item.CloneDefaults(ItemID.BorealWoodSword);
	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Drywood>(), 7).AddTile(TileID.WorkBenches).Register();
}
