using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class CrystalFish : FoodItem
{
	internal override Point Size => new(48, 34);

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.MagicPower, 9860);
		return true;
	}

	public override void Defaults()
	{
		Item.rare = ItemRarityID.Orange;
		Item.buffType = BuffID.WellFed2;
		Item.buffTime = 17 * 60 * 60;
		Item.value = Item.sellPrice(0, 0, 2, 0);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<RawFish>())
		.AddIngredient(ItemID.CrystalShard).AddTile(TileID.CookingPots).Register();
}

