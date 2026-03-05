using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Items.DrywoodSet;

public class DrywoodBow : ModItem
{
	public override void SetDefaults() => Item.CloneDefaults(ItemID.BorealWoodBow);
	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Drywood>(), 10).AddTile(TileID.WorkBenches).Register();
}
