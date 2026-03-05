using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Underground.Items;

public class SandBomb : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DirtBomb);
		Item.shoot = ModContent.ProjectileType<SandBombProjectile>();
		Item.notAmmo = true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bomb).AddIngredient(ItemID.SandBlock, 25).Register();
}

public class SandBombProjectile : SpreadBomb
{
	public override string Texture => base.Texture.Replace("Projectile", string.Empty);
	public override LocalizedText DisplayName => Language.GetText($"Mods.SpiritReforged.Items.{Name.Replace("Projectile", string.Empty)}.DisplayName");

	public override void SetDefaults()
	{
		base.SetDefaults();

		dustType = DustID.Sand;
		tileType = TileID.Sand;
	}
}