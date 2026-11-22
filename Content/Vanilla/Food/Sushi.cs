using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Content.Savanna.NPCs.Gar;
using SpiritReforged.Content.Savanna.NPCs.Killifish;

namespace SpiritReforged.Content.Vanilla.Food;

public class Sushi : FoodItem
{
	internal override Point Size => new(26, 24);

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.Gills, 1800);
		return true;
	}

	public override void AddRecipes()
	{
		AddRecipe(ItemID.RedSnapper);
		AddRecipe(ItemID.Salmon);
		AddRecipe(ItemID.Trout);
		AddRecipe(ItemID.AtlanticCod);
		AddRecipe(AutoContent.ItemType<Killifish>());
		AddRecipe(AutoContent.ItemType<Gar>());

		void AddRecipe(int ingredient) => CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(ingredient).AddTile(TileID.CookingPots).Register();
	}
}