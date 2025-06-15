using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;
public class GoldenCaviar : FoodItem
{
	internal override Point Size => new(30, 34);

	public override void StaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.GoldenDelight;

	public override void Defaults()
	{
		Item.buffType = BuffID.WellFed3;
		Item.buffTime = 50 * 60 * 60;
		Item.value = Item.sellPrice(0, 2, 0, 0);
	}

	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(Mod.Find<ModItem>("GoldGarItem").Type).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(Mod.Find<ModItem>("GoldKillifishItem").Type).AddTile(TileID.CookingPots).Register();
	}
}

