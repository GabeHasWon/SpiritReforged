using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class FishChips : FoodItem
{
	internal override Point Size => new(42, 30);

	public override void Defaults()
	{
		Item.buffTime = 7 * 60 * 60;
		Item.value = Item.sellPrice(0, 0, 2, 0);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<RawFish>(), 5).AddTile(TileID.CookingPots).Register();
}

