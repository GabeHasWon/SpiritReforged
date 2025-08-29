using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Content.Savanna.NPCs.Gar;
using SpiritReforged.Content.Savanna.NPCs.Killifish;

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

	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(ItemID.BottledHoney, 1).AddIngredient(ItemID.RedSnapper, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ItemID.BottledHoney, 1).AddIngredient(ItemID.Salmon, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ItemID.BottledHoney, 1).AddIngredient(ItemID.Trout, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ItemID.BottledHoney, 1).AddIngredient(ItemID.AtlanticCod, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ItemID.BottledHoney, 1).AddIngredient(AutoContent.ItemType<Killifish>(), 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ItemID.BottledHoney, 1).AddIngredient(AutoContent.ItemType<Gar>(), 1).AddTile(TileID.CookingPots).Register();
	}
}

