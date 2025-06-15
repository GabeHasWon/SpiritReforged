using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class CoconutMilk : FoodItem
{
	internal override Point Size => new(24, 30);

	public override void Defaults()
	{
		Item.buffTime = 10 * 60 * 60;
		Item.useStyle = ItemUseStyleID.DrinkLiquid;
		Item.UseSound = SoundID.Item3;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Coconut).AddTile(TileID.CookingPots).Register();
}

