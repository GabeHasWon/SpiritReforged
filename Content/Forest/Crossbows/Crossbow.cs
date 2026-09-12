using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Crossbow : ModItem
{
	public class CrossbowHeld : SwungProjectile
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Crossbow>().DisplayName;

		public override string Texture => ModContent.GetInstance<Crossbow>().Texture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(null, 30, 10);

		public override void AI()
		{
			if (Counter == 0)
			{
				for (int i = 0; i < 8; i++)
					Dust.NewDustPerfect(GetEndPosition(-8), Main.rand.NextFromList(DustID.GoldCoin, DustID.Smoke), (Vector2.Normalize(Projectile.velocity) * Main.rand.NextFloat(3f)).RotatedByRandom(1f)).noGravity = true;
			}

			base.AI();
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (Projectile.direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

			float holdDistance = 8 + 20 * (1f - Math.Min(Progress * 3, 1));
			Vector2 origin = new(holdDistance, texture.Height / 2);

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			return false;
		}

		public override bool? CanDamage() => false;
	}

	public override void SetDefaults()
    {
		Item.DefaultToBow(30, 10, true);
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<CrossbowHeld>(), damage, knockback, player, 0, source);
		return true;
    }
}