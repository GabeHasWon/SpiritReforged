using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Wrenches;

public class MagnetiteWrench : ModItem
{
	private class MagnetiteBoostProjectile : GlobalProjectile, IWrenchGlobal
	{
		public override bool InstancePerEntity => true;

		public int Duration { get; set; }

		public override void AI(Projectile projectile)
		{
			if (Duration > 0)
			{
				Duration--;
				projectile.GetGlobalProjectile<SpeedModifierProjectile>().SpeedModifier += 0.25f;

				IWrenchGlobal.ClientPassiveEffects(projectile, 0.5f);

				//Spawn shockwaves
			}
		}

		public override void PostDraw(Projectile projectile, Color lightColor)
		{
			if (Duration > 0)
				IWrenchGlobal.DrawDurationBar(projectile, Duration / (5 * 60f));
		}
	}

	public class MagnetiteWrenchSwing : CopperSpanner.CopperSpannerSwing, IDrawPixelated, IHitSentry
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<MagnetiteWrench>().DisplayName;
		public override string Texture => ModContent.GetInstance<MagnetiteWrench>().Texture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 50, 25);

		void IHitSentry.OnHitSentry(Player player, Projectile sentry, ref int cooldown)
		{
			IHitSentry.ClientHitEffects(sentry);

			player.GetModPlayer<WrenchPlayer>().StoredScrap--;
			sentry.GetGlobalProjectile<MagnetiteBoostProjectile>().Duration = 5 * 60;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (!_recoiling)
				DrawPixelatedSmear(spriteBatch, new Color(187, 165, 124));
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 12;
		Item.useTime = Item.useAnimation = 22;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.DamageType = ModContent.GetInstance<WrenchClass>();
		Item.knockBack = 4;
		Item.useTurn = true;
		Item.rare = ItemRarityID.Blue;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<MagnetiteWrenchSwing>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 3, source);
		return false;
	}
}