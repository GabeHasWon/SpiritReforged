using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Snow;

public class Claymore : ModItem
{
	public sealed class ClaymoreSwing : SwungProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Claymore>().DisplayName;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCircularOut, 110, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + (MathHelper.PiOver4 - EaseFunction.EaseCubicIn.Ease(Progress) * 1.2f) * SwingDirection;
		}

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool())
				Dust.NewDustPerfect(GetEndPosition(-30) + Main.rand.NextVector2Circular(50, 50) * Main.rand.NextFloat(), DustID.Smoke, Projectile.velocity, 100).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{

		}

		public override bool PreDraw(ref Color lightColor)
		{
			const int handle = 12;

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(handle, effects == SpriteEffects.FlipVertically ? handle : texture.Height - handle); //The handle

			for (int x = 0; x < 4; x++)
			{
				float rotation = Projectile.rotation - SwingDirection * 0.3f * x * GetConfig<BasicConfiguration>().Easing.Ease(1f - Math.Min(Progress * 2, 1));
				DrawHeld(lightColor * (1f - x / 3f) * 0.5f, origin, rotation, effects);
			}

			DrawHeld(lightColor, origin, Projectile.rotation, effects);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => DrawPixelatedSmear(spriteBatch, Color.Gray);

		public void DrawPixelatedSmear(SpriteBatch spriteBatch, Color color)
		{
			Player owner = Main.player[Projectile.owner];

			//Draw a custom smear
			Main.instance.LoadProjectile(985);
			Texture2D smear = TextureAssets.Projectile[985].Value;

			SpriteEffects effects = SwingDirection == -1 ? SpriteEffects.FlipVertically : default;
			Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
			float rotation = Projectile.rotation - (MathHelper.PiOver4 + Progress) * SwingDirection;

			Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Vector2 origin = new(source.Width, source.Height / 2);
			Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation);
			Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

			float scale = GetConfig<BasicConfiguration>().Reach / 250f;
			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)), rotation, origin, scale, effects, 0);
			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)).Additive(100), rotation, origin, scale * 0.8f, effects, 0);
		}
	}

	private bool _reverseSwing;

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 30;
		Item.useTime = Item.useAnimation = 35;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.DamageType = DamageClass.Melee;
		Item.knockBack = 4;
		Item.useTurn = true;
		Item.rare = ItemRarityID.Blue;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<ClaymoreSwing>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, (_reverseSwing = !_reverseSwing) ? -5f : 5f, source);
		return false;
	}
}