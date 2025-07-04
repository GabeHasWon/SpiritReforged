namespace SpiritReforged.Content.Underground.Items;

public class SandBombSticky : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DirtBomb);
		Item.shoot = ModContent.ProjectileType<SandBombStickyProjectile>();
		Item.notAmmo = true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.StickyBomb).AddIngredient(ItemID.SandBlock, 25).Register();
}

public class SandBombStickyProjectile : SandBombProjectile
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		sticky = true;
	}
}