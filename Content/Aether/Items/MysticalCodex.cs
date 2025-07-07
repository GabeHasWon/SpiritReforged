namespace SpiritReforged.Content.Aether.Items;

internal class MysticalCodex : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Radar);
		Item.Size = new(30, 32);
	}

	public override void UpdateInfoAccessory(Player player)
	{
		player.GetModPlayer<LedgerPlayer>().Enabled = true;
		player.GetModPlayer<ScryingPlayer>().Enabled = true;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient<Ledger>()
		.AddIngredient<ScryingLens>()
		.AddTile(TileID.TinkerersWorkbench)
		.Register();
}
