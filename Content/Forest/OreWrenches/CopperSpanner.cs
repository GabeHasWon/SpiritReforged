using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.OreWrenches;

public class CopperSpanner : ModItem, IHitSentry
{
	public sealed class SpeedUpProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		internal short empoweredTime = 0;

		public override bool PreAI(Projectile projectile)
		{
			empoweredTime = (short)Math.Max(empoweredTime - 1, 0);

			if (empoweredTime > 0)
			{
				projectile.GetGlobalProjectile<SpeedModifierProjectile>().speed += 0.12f;

				if (Main.rand.NextBool(16))
					Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Electric);
			}

			return true;
		}
	}

	public class CopperSpannerSwing : SwungProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<CopperSpanner>().DisplayName;

		public override string Texture => ModContent.GetInstance<CopperSpanner>().Texture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 40, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + (MathHelper.PiOver4 - Progress) * SwingDirection;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => IHitSentry.DropScrap(Main.player[Projectile.owner], target);

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, (effects == SpriteEffects.FlipVertically) ? (TextureAssets.Projectile[Type].Value.Height - 34) : 34); //The handle

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => DrawPixelatedSmear(spriteBatch, new Color(183, 88, 35));

		public void DrawPixelatedSmear(SpriteBatch spriteBatch, Color color)
		{
			Player owner = Main.player[Projectile.owner];

			//Draw a custom smear
			Main.instance.LoadProjectile(985);
			Texture2D smear = TextureAssets.Projectile[985].Value;

			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
			float rotation = Projectile.rotation - MathHelper.PiOver2 * SwingDirection + SwingDirection * Progress;

			Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Vector2 origin = new(source.Width, source.Height / 2);
			Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation);
			Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)), rotation, origin, 0.25f, effects, 0);
			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)).Additive(100), rotation, origin, 0.2f, effects, 0);
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
		Item.shoot = ModContent.ProjectileType<CopperSpannerSwing>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 3, source);
		return false;
	}

	bool IHitSentry.CanHitSentry(Player player, Projectile sentry) => player.GetModPlayer<WrenchPlayer>().StoredScrap > 0;

	void IHitSentry.OnHitSentry(Player player, Projectile sentry) 
	{
		const int duration = 5 * 60;

		player.GetModPlayer<WrenchPlayer>().StoredScrap--;
		sentry.GetGlobalProjectile<SpeedUpProjectile>().empoweredTime = duration;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.CopperBar, 12).AddTile(TileID.Anvils).Register();
}