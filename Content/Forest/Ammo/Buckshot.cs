using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Ammo;
public class Buckshot : ShotgunAmmoItem
{
	static void Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		for (int i = 0; i < shotCount; i++)
		{
			Vector2 spreadDir = direction;

			if (spreadAmount > 0f && i != 0) // no spread on first shot
				spreadDir = direction.RotatedByRandom(spreadAmount);

			PreNewProjectile.New(source, position, spreadDir * speed * Main.rand.NextFloat(0.75f, 1.5f), ModContent.ProjectileType<ShotProjectile>(), damage, knockback, player.whoAmI, preSpawnAction: (projectile) =>
			{
				(projectile.ModProjectile as ShotProjectile).BaseColor = new(255, 90, 0); // tint bullets slightly more red
			});

			for (int x = 0; x < 2; x++)
				Dust.NewDustPerfect(position, DustID.Torch, direction.RotatedByRandom(spreadAmount * 1.25f) * Main.rand.NextFloat(speed, speed * 3f), 0, default, Main.rand.NextFloat(1.5f)).noGravity = true;

			Dust.NewDustPerfect(position + direction * speed, DustID.Smoke, direction.RotatedByRandom(0.4f) * Main.rand.NextFloat(3f), 240, default, Main.rand.NextFloat(3f, 6f));
		}
	}

	public Buckshot() : base(Behavior, 9, .65f, 13f) { }

	public override void SafeSetDefaults()
	{
		Item.damage = 8;
		Item.knockBack = 3f;
	}

	public override void AddRecipes()
	{
		CreateRecipe(50).
			AddIngredient<Shot>(50).
			AddRecipeGroup(RecipeGroupID.IronBar, 2).
			AddTile(TileID.Anvils).
			Register();
	}
}
