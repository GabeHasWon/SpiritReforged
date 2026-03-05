using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
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
		AddRecipe(ItemID.RedSnapper);
		AddRecipe(ItemID.Salmon);
		AddRecipe(ItemID.Trout);
		AddRecipe(ItemID.AtlanticCod);
		AddRecipe(AutoContent.ItemType<Killifish>());
		AddRecipe(AutoContent.ItemType<Gar>());

		void AddRecipe(int ingredient) => CreateRecipe().AddIngredient(ItemID.BottledHoney).AddIngredient(ingredient).AddTile(TileID.CookingPots).Register();
	}
}