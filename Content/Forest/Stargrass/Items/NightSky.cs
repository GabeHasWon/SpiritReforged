using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Forest.Stargrass.Items;

public class NightSky : FoodItem
{
	internal override Point Size => new(18, 32);

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.ManaRegeneration, 36000);
		return true;
	}

	public override void Defaults()
	{
		Item.buffTime = 20 * 60 * 60;
		Item.useStyle = ItemUseStyleID.DrinkLiquid;
		Item.UseSound = SoundID.Item3;
		Item.buffType = BuffID.WellFed2;
		Item.value = Item.sellPrice(silver: 20);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<MidnightApple>())
		.AddIngredient(ModContent.ItemType<CrescentMelon>()).AddIngredient(ModContent.ItemType<Pearlberry>())
		.AddIngredient(ItemID.Bottle).AddTile(TileID.CookingPots).Register();
}
