namespace SpiritReforged.Content.Crossmod.Spooky.Items;

internal class BallOfBandages : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Star);
		Item.rare = ItemRarityID.Quest;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient<MummyBandageScrap>(6).Register();
}
