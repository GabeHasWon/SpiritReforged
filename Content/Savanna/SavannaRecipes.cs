namespace SpiritReforged.Content.Savanna;

public class SavannaRecipes : ModSystem
{
	public override void AddRecipes()
	{
		Recipe.Create(ItemID.HunterPotion).AddIngredient(ItemID.BottledWater).AddIngredient(ItemID.Blinkroot)
			.AddIngredient(Mod.Find<ModItem>("GarItem").Type).AddTile(TileID.Bottles).Register();

		Recipe.Create(ItemID.RoastedBird).AddIngredient(Mod.Find<ModItem>("SparrowItem").Type)
			.AddTile(TileID.CookingPots).Register();
	}
}
