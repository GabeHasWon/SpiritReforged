using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Savanna.Items.Food;

public class Omelette : FoodItem
{
	internal override Point Size => new(36, 24);

	public override void Defaults()
	{
		Item.buffType = BuffID.WellFed2;
		Item.buffTime = 10 * 60 * 60;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<OstrichEgg>())
			.AddTile(TileID.CookingPots).Register();
}

