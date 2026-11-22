namespace SpiritReforged.Content.Forest.Stand;

public class SwordStandItem : ModItem
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Wood, 12).AddTile(TileID.Sawmill).Register();
	public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<SwordStand>(), 0);
}

public class SwordStandSandstoneItem : ModItem
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.SmoothSandstone, 12).AddTile(TileID.Sawmill).Register();
	public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<SwordStand>(), 1);
}

public class SwordStandMahoganyItem : ModItem
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.RichMahogany, 12).AddTile(TileID.Sawmill).Register();
	public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<SwordStand>(), 2);
}