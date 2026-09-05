using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underworld.Blasphemer;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Snow;

public class Claymore : ModItem
{
	public sealed class ClaymoreSwing : SwungProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Claymore>().DisplayName;

		public float SineEasing
		{
			get
			{
				float maxProgress = Math.Max(Progress - 0.3f, 0);
				float minResult = Math.Min(maxProgress / 0.5f, 1);

				return (float)EaseFunction.EaseSine.Ease(minResult);
			}
		}

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseQuinticInOut, 110, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + MathHelper.PiOver4 * SwingDirection;
		}

		public override void AI()
		{
			base.AI();

			Projectile.scale = 0.8f + EaseFunction.EaseSine.Ease(Progress) / 4f;

			if (Counter == (int)(SwingTime / 3))
				SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = 0.5f }, Projectile.Center);

			if (SineEasing > 0.5f && Main.rand.NextBool())
			{
				Dust dust = Dust.NewDustPerfect(GetEndPosition(-30) + Main.rand.NextVector2Circular(50, 50) * Main.rand.NextFloat(), DustID.TreasureSparkle, new Vector2(1, 0).RotatedBy(Projectile.rotation));
				dust.noLight = true;
				dust.noLightEmittence = true;
				dust.scale = 0.5f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			float rotation = Main.rand.NextFloat(-MathHelper.PiOver2 - 0.5f, -MathHelper.PiOver2 + 0.5f);
			for (int i = 0; i < 2; i++)
			{
				Vector2 velocity = ((i == 0) ? Vector2.UnitX : -Vector2.UnitX).RotatedBy(rotation) * 3;
				ParticleHandler.SpawnParticle(new CartoonHit(target.Center, 20, 1.2f, MathHelper.PiOver4 * 5 + velocity.ToRotation(), velocity));
			}

			SoundEngine.PlaySound(SoundID.NPCHit18 with { Pitch = -0.5f }, target.Center);
			SoundEngine.PlaySound(BlasphemerProj.Impact2 with { Volume = 0.3f, Pitch = 0.8f }, target.Center);
		}

		public override bool? CanDamage() => (SineEasing > 0.5f) ? base.CanDamage() : false;

		public override bool PreDraw(ref Color lightColor)
		{
			const int handle = 12;

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(handle, effects == SpriteEffects.FlipVertically ? handle : texture.Height - handle); //The handle

			for (int x = 0; x < 4; x++)
			{
				float rotation = Projectile.rotation - SwingDirection * 0.5f * x * SineEasing;
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
			float progress = (Progress - 0.5f) / 0.5f;

			SpriteEffects effects = SwingDirection == -1 ? SpriteEffects.FlipVertically : default;
			Rectangle source = smear.Frame(1, 4, 0, (int)(progress * 14f));
			float rotation = Projectile.rotation - (MathHelper.PiOver4 + progress) * SwingDirection;

			Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Color finalColor = Projectile.GetAlpha(lightColor.MultiplyRGB(color));
			Vector2 origin = new(source.Width, source.Height / 2);

			float scale = GetConfig<BasicConfiguration>().Reach / 250f;

			spriteBatch.Draw(smear, GetPosition(rotation - progress * SwingDirection * 2), source, finalColor * 0.7f, rotation - progress * SwingDirection * 2, origin, scale, effects, 0);
			spriteBatch.Draw(smear, GetPosition(rotation), source, finalColor, rotation, origin, scale, effects, 0);
			spriteBatch.Draw(smear, GetPosition(rotation), source, finalColor.Additive(100), rotation, origin, scale * 0.8f, effects, 0);

			Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
			spriteBatch.Draw(star, GetPosition(Projectile.rotation - MathHelper.PiOver4 * SwingDirection), null, Color.White.Additive(), 0, star.Size() / 2, scale * 0.2f * SineEasing, 0, 0);

			Vector2 GetPosition(float rotation)
			{
				Vector2 value = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation) - Main.screenPosition;
				IDrawPixelated.PixelateDrawPosition(ref value);

				return value;
			}
		}
	}

	private bool _reverseSwing;

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 30;
		Item.useTime = Item.useAnimation = 50;
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