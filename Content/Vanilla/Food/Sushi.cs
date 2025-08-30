using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Content.Ocean.Tiles;
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
		CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(ItemID.RedSnapper, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(ItemID.Salmon, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(ItemID.Trout, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(ItemID.AtlanticCod, 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(AutoContent.ItemType<Killifish>(), 1).AddTile(TileID.CookingPots).Register();
		CreateRecipe().AddIngredient(ModContent.ItemType<Kelp>(), 5).AddIngredient(AutoContent.ItemType<Gar>(), 1).AddTile(TileID.CookingPots).Register();
	}
}
