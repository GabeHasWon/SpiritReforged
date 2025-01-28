using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Items;

namespace SpiritReforged.Content.Vanilla.Food;

public class Sushi : FoodItem
{
	internal override Point Size => new(44, 28);

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.Gills, 1800);
		return true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5)
		.AddIngredient(ModContent.ItemType<RawFish>()).AddTile(TileID.CookingPots).Register();
}
