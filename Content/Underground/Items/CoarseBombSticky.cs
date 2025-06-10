using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Underground.Items;

public class CoarseBombSticky : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DirtBomb);
		Item.shoot = ModContent.ProjectileType<CoarseBombStickyProjectile>();
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.StickyBomb).AddIngredient(AutoContent.ItemType<SavannaDirt>(), 25).Register();
}

public class CoarseBombStickyProjectile : CoarseBombProjectile
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		sticky = true;
	}
}