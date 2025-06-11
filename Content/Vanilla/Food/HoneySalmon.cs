using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class HoneySalmon : FoodItem
{
	internal override Point Size => new(52, 38);

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.Honey, 1800);
		return true;
	}

	public override void Defaults()
	{
		Item.rare = ItemRarityID.Green;
		Item.buffType = BuffID.WellFed2;
		Item.buffTime = 9 * 60 * 60;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<RawFish>(), 2)
		.AddIngredient(ItemID.BottledHoney).AddTile(TileID.CookingPots).Register();
}

