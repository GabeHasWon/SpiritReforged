using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class CookedMeat : FoodItem
{
	public override void StaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<RawMeat>();

	internal override Point Size => new(30, 24);

	public override void Defaults() => Item.buffTime = 6 * 60 * 60;

	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(ModContent.ItemType<RawMeat>()).AddTile(TileID.Campfire).Register();
		CreateRecipe(2).AddIngredient(ModContent.ItemType<RawMeat>()).AddTile(TileID.CookingPots).Register();
	}
}

