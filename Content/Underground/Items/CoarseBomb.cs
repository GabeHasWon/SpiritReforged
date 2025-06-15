using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Underground.Items;

public class CoarseBomb : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DirtBomb);
		Item.shoot = ModContent.ProjectileType<CoarseBombProjectile>();
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bomb).AddIngredient(AutoContent.ItemType<SavannaDirt>(), 25).Register();
}

public class CoarseBombProjectile : SpreadBomb
{
	public override string Texture => base.Texture.Replace("Projectile", string.Empty);
	public override LocalizedText DisplayName => Language.GetText($"Mods.SpiritReforged.Items.{Name.Replace("Projectile", string.Empty)}.DisplayName");

	public override void SetDefaults()
	{
		base.SetDefaults();

		area = 4;
		dustType = DustID.Dirt;
		tileType = ModContent.TileType<SavannaDirt>();
	}
}