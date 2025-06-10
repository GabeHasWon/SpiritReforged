using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Items.DrywoodSet;

public class DrywoodHammer : ModItem
{
	public override void SetDefaults() => Item.CloneDefaults(ItemID.BorealWoodHammer);
	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Drywood>(), 8).AddTile(TileID.WorkBenches).Register();
}
