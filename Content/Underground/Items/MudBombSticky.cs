namespace SpiritReforged.Content.Underground.Items;

public class MudBombSticky : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DirtBomb);
		Item.shoot = ModContent.ProjectileType<MudBombStickyProjectile>();
		Item.notAmmo = true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.StickyBomb).AddIngredient(ItemID.MudBlock, 25).Register();
}

public class MudBombStickyProjectile : MudBombProjectile
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		sticky = true;
	}
}