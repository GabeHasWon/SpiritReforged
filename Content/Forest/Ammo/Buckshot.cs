using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Ammo;

public class Buckshot : ModItem, IShotgunAmmo
{
	public ShotgunStats Stats => new(ShotgunStats.Additive(4), ShotgunStats.Multiple(0.3f));

	public void OnShoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback, int index)
	{
		if (index == 0)
			velocity = position.DirectionTo(Main.MouseWorld) * velocity.Length(); //Remove all spread from the first shot

		PreNewProjectile.New(source, position, velocity * Main.rand.NextFloat(0.75f, 1.5f), ModContent.ProjectileType<ShotProjectile>(), damage, knockback, player.whoAmI, preSpawnAction: (projectile) =>
		{
			projectile.scale += 0.2f + Main.rand.NextFloat(0.5f);
			(projectile.ModProjectile as ShotProjectile).BaseColor = new(255, 130, 0); // tint bullets slightly more red
		});

		for (int x = 0; x < 2; x++)
			Dust.NewDustPerfect(position, DustID.Torch, velocity.RotatedByRandom(Stats.GetShotSpread(player) * 1.25f) * Main.rand.NextFloat(1f, 3f), 0, default, Main.rand.NextFloat(1.5f)).noGravity = true;

		Dust.NewDustPerfect(position + velocity, DustID.Smoke, Vector2.Normalize(velocity).RotatedByRandom(0.4f) * Main.rand.NextFloat(3f), 240, default, Main.rand.NextFloat(3f, 6f));
	}

	public override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<ShotgunClass>();
		Item.ammo = ModContent.ItemType<Shot>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.shootSpeed = 18;
		Item.damage = 9;
		Item.knockBack = 6f;
	}

	public override void AddRecipes() => CreateRecipe(50).AddIngredient<Shot>(50).AddRecipeGroup(RecipeGroupID.IronBar, 2).AddTile(TileID.Anvils).Register();
}