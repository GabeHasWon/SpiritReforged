using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Content.Ocean.Items;

namespace SpiritReforged.Content.Vanilla.Food;
public class Nigiri : FoodItem
{
	internal override Point Size => new(44, 28);

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.Flipper, 3600);
		return true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 7)
		.AddIngredient(ModContent.ItemType<RawFish>()).AddTile(TileID.CookingPots).Register();
}

