namespace SpiritReforged.Content.Underground.Items;

public class ClayBombSticky : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DirtBomb);
		Item.shoot = ModContent.ProjectileType<ClayBombStickyProjectile>();
		Item.notAmmo = true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.StickyBomb).AddIngredient(ItemID.ClayBlock, 25).Register();
}

public class ClayBombStickyProjectile : ClayBombProjectile
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		sticky = true;
	}
}