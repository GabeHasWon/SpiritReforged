using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Content.Ocean.Items;

namespace SpiritReforged.Content.Vanilla.Food;

public class SteamedMussels : FoodItem
{
	internal override Point Size => new(46, 22);

	public override void AddRecipes() => CreateRecipe().AddIngredient(Mod.Find<ModItem>("MusselItem").Type, 3)
		.AddIngredient(ModContent.ItemType<Kelp>()).AddTile(TileID.CookingPots).Register();
}
