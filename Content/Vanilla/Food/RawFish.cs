using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class RawFish : FoodItem
{
	internal override Point Size => new(34, 22);

	public override void StaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<RawMeat>();

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.Poisoned, 600);
		return true;
	}

	public override void AddRecipes()
	{
		Recipe.Create(ItemID.CookedFish).AddIngredient(ModContent.ItemType<RawFish>()).AddTile(TileID.CookingPots).Register();
		Recipe.Create(ItemID.Sashimi).AddIngredient(ModContent.ItemType<RawFish>()).AddTile(TileID.WorkBenches).Register();
	}
}
