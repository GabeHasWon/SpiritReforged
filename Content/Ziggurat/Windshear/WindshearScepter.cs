using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Ziggurat.Windshear;

[AutoloadGlowmask("255,255,255")]
public class WindshearScepter : ModItem
{
	[Autoload(Side = ModSide.Client)]
	public sealed class CloudMetaballSystem : ModSystem
	{
		private static readonly ModTarget2D CloudTarget = new(static () => CloudParticleRenderer.Particles.Count != 0 || Data.Count != 0, DrawCloudTarget);
		private static readonly ParticleRenderer CloudParticleRenderer = new();
		private static readonly HashSet<DrawData> Data = [];

		public static void Add(ABasicParticle particle) => CloudParticleRenderer.Add(particle);
		public static void Add(DrawData drawData) => Data.Add(drawData);

		public override void Load() => On_Main.DrawProjectiles += DrawShader;

		private static void DrawShader(On_Main.orig_DrawProjectiles orig, Main self)
		{
			orig(self);

			if (CloudTarget != null && CloudTarget.Active)
			{
				Effect s = AssetLoader.LoadedShaders["CloudMetaball"].Value;
				SpriteBatch spriteBatch = Main.spriteBatch;

				s.Parameters["primaryColor"].SetValue(Color.White.ToVector4());
				s.Parameters["secondaryColor"].SetValue(new Color(0.3f, 0.3f, 0.5f).ToVector4());
				s.Parameters["numColors"].SetValue(3);
				ShaderHelpers.SetEffectMatrices(ref s);

				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

				spriteBatch.Draw(CloudTarget, Vector2.Zero, Color.White);

				spriteBatch.End();
			}
		}

		private static void DrawCloudTarget(SpriteBatch spriteBatch)
		{
			CloudParticleRenderer.Settings.AnchorPosition = -Main.screenPosition;
			CloudParticleRenderer.Draw(spriteBatch);

			foreach (DrawData data in Data)
				spriteBatch.Draw(data.texture, data.position, data.sourceRect, data.color, data.rotation, data.origin, data.scale, data.effect, 0);

			Data.Clear();
		}

		public override void PostUpdateProjectiles() => CloudParticleRenderer.Update();
	}

	public class CloudParticle(int style) : ABasicParticle
	{
		public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal(typeof(CloudParticle), "CloudParticle", false);

		public float Opacity;
		public int TimeToLive = 60;

		private readonly int _style = style;
		private int _timeSinceSpawn;

		public override void Update(ref ParticleRendererSettings settings)
		{
			base.Update(ref settings);

			if (++_timeSinceSpawn > TimeToLive)
			{
				ShouldBeRemovedFromRenderer = true;
			}
			else
			{
				Velocity *= 0.99f;
				Rotation += Velocity.X * 0.01f;

				int halfTime = TimeToLive / 2;

				if (_timeSinceSpawn > halfTime)
					Opacity -= 1f / halfTime;
			}
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
		{
			Texture2D texture = Texture.Value;
			Vector2 position = settings.AnchorPosition + LocalPosition;
			float frame = (float)_timeSinceSpawn / TimeToLive;
			Rectangle source = texture.Frame(2, 5, _style, (int)(EaseFunction.EaseCubicIn.Ease(frame) * 5f), -2, -2);

			spritebatch.Draw(texture, position, source, Color.White * Opacity, Rotation, source.Size() / 2, Scale, default, 0);
		}
	}

	public class WindshearScepterSwing : SwungProjectile
	{
		public override string Texture => ModContent.GetInstance<WindshearScepter>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<WindshearScepter>().DisplayName;

		public override Configuration SetConfiguration() => new(new PolynomialEase(static(x) => Math.Min(x * 3, 1)), 30, 10);
		public override bool? CanDamage() => false;

		public override float GetRotation(out float armRotation)
		{
			if (Progress < 0.3f && Main.rand.NextBool())
			{
				Vector2 endPosition = Projectile.Center + new Vector2(config.Reach, -config.Reach).RotatedBy(Projectile.rotation);
				var dust = Dust.NewDustPerfect(endPosition, DustID.YellowTorch, new Vector2(1, SwingDirection).RotatedBy(Projectile.rotation) * Main.rand.NextFloat(2f));
				dust.noGravity = true;
				dust.noLightEmittence = true;
			}

			int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
			float value = base.GetRotation(out armRotation) + direction * Progress * 2.5f;

			return value + MathHelper.PiOver4;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int originLength = (int)MathHelper.Lerp(2, 20, Progress);
			Vector2 origin = new(originLength, TextureAssets.Projectile[Type].Value.Height - originLength);
			Texture2D glowmask = GlowmaskItem.ItemIdToGlowmask[ModContent.ItemType<WindshearScepter>()].Glowmask.Value;

			float opacity = (1f - Progress - 0.7f) / 0.3f * Projectile.Opacity;
			int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
			SpriteEffects effects = direction == -1 ? SpriteEffects.FlipVertically : default;

			DrawHeld(lightColor, origin, Projectile.rotation);
			Main.EntitySpriteDraw(glowmask, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Projectile.GetAlpha(Color.Lerp(Color.White, Color.Red, opacity)), Projectile.rotation, origin, Projectile.scale, default, 0);
			
			DrawSmear(Projectile.GetAlpha(Color.Goldenrod.Additive(100)) * 0.3f * opacity, Projectile.rotation - MathHelper.PiOver4, (int)(Progress * 8f), config.Reach + 38, 0.5f, effects);
			DrawSmear(Projectile.GetAlpha(Color.White.Additive(100)) * 0.3f * opacity, Projectile.rotation - MathHelper.PiOver4, (int)(Progress * 8f), config.Reach + 38, 0.4f, effects);

			if (opacity > 0)
			{
				Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
				Effect blurEffect = AssetLoader.LoadedShaders["BlurLine"].Value;
				Vector2 endPosition = Projectile.Center + new Vector2(config.Reach, -config.Reach).RotatedBy(Projectile.rotation);

				Main.spriteBatch.Draw(bloom, endPosition - Main.screenPosition, null, Color.PaleGoldenrod.Additive() * 0.3f * opacity, MathHelper.PiOver2, bloom.Size() / 2, Projectile.scale * 0.3f, default, 0);

				for (int i = 0; i < 2; i++)
				{
					float rotation = MathHelper.PiOver2 * i;

					PrimitiveRenderer.DrawPrimitiveShape(new SquarePrimitive()
					{
						Position = endPosition - Main.screenPosition,
						Height = 50,
						Length = 20,
						Rotation = rotation,
						Color = Color.Goldenrod * opacity
					}, blurEffect);

					PrimitiveRenderer.DrawPrimitiveShape(new SquarePrimitive()
					{
						Position = endPosition - Main.screenPosition,
						Height = 50,
						Length = 10,
						Rotation = rotation,
						Color = Color.White * opacity
					}, blurEffect);
				}
			}

			return false;
		}
	}

	public class WindBlast : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.timeLeft = 40;
			Projectile.Opacity = 0;
			Projectile.penetrate = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (!Main.dedServ && Projectile.timeLeft > 15)
			{
				Rectangle hitbox = Projectile.Hitbox;
				Vector2 position = Main.rand.NextVector2FromRectangle(hitbox) + Projectile.velocity;
				Vector2 velocity = Projectile.velocity * Main.rand.NextFloat(0.2f, 0.6f);

				CloudMetaballSystem.Add(new CloudParticle(Main.rand.Next(2))
				{
					LocalPosition = position,
					Scale = Vector2.One * Projectile.scale * Main.rand.NextFloat(0.8f, 1.2f),
					Velocity = velocity,
					TimeToLive = 20,
					Opacity = 1,
					Rotation = Main.rand.NextFloat()
				});
			}

			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 0.97f;

			if (Projectile.timeLeft < 10)
				Projectile.Opacity -= 0.1f;
			else if (Projectile.Opacity < 1)
				Projectile.Opacity += 0.1f;
		}

		public override void ModifyDamageHitbox(ref Rectangle hitbox) => hitbox.Inflate(20, 20);

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ || timeLeft == 0)
				return;

			for (int i = 0; i < 3; i++)
				CloudMetaballSystem.Add(new CloudParticle(Main.rand.Next(2))
				{
					LocalPosition = Projectile.Center + Projectile.velocity,
					Scale = Vector2.One * Projectile.scale,
					Velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(),
					TimeToLive = 20,
					Rotation = Main.rand.NextFloat(),
					Opacity = 1
				});

			SoundEngine.PlaySound(SoundID.DD2_SonicBoomBladeSlash, Projectile.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			const int type = 684;
			const int trailLength = 5;

			Main.instance.LoadProjectile(type);
			Texture2D texture = TextureAssets.Projectile[type].Value;
			Rectangle source = texture.Frame();

			float fadeIn = MathHelper.Lerp(1, 3, Projectile.Opacity);
			Vector2 scale = new Vector2(1, fadeIn + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 40f) * 0.5f) * Projectile.scale * 0.5f;

			for (int i = 0; i < trailLength; i++)
			{
				Color color = Color.White * Projectile.Opacity * (1f - i / (trailLength - 1f));
				Vector2 position = Projectile.Center - Main.screenPosition - Projectile.velocity * i;

				CloudMetaballSystem.Add(new DrawData(texture, position, source, color, Projectile.rotation + MathHelper.PiOver2, source.Size() / 2, scale, default, 0));
			}

			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			Effect blurEffect = AssetLoader.LoadedShaders["BlurLine"].Value;
			float bloomOpacity = Projectile.Opacity;

			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * 0.3f * bloomOpacity, MathHelper.PiOver2, bloom.Size() / 2, Projectile.scale * 0.5f, default, 0);

			PrimitiveRenderer.DrawPrimitiveShape(new SquarePrimitive()
			{
				Position = Projectile.Center - Main.screenPosition,
				Height = 100 * scale.Y,
				Length = 50 * scale.X,
				Rotation = MathHelper.PiOver2,
				Color = Color.White * bloomOpacity
			}, blurEffect);

			PrimitiveRenderer.DrawPrimitiveShape(new SquarePrimitive()
			{
				Position = Projectile.Center - Main.screenPosition,
				Height = 100 * scale.Y,
				Length = 16 * scale.X,
				Rotation = MathHelper.PiOver2,
				Color = Color.Goldenrod * bloomOpacity
			}, blurEffect);

			return false;
		}
	}

	private float _swingArc = 1;

	public override void SetDefaults()
	{
		Item.damage = 40;
		Item.mana = 15;
		Item.knockBack = 6.5f;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 34;
		Item.DamageType = DamageClass.Magic;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.DD2_BookStaffCast with { Pitch = 0.3f };
		Item.shoot = ModContent.ProjectileType<WindBlast>();
		Item.shootSpeed = 14f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		Vector2 offset = Vector2.Normalize(velocity) * 30;

		if (Collision.CanHit(position, 2, 2, position + velocity, 2, 2))
			position += offset;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SoundEngine.PlaySound(SoundID.Item19 with { Volume = 0.7f, Pitch = 1f }, player.Center);
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<WindshearScepterSwing>(), damage, knockback, player, _swingArc *= -1, source);

		return true;
	}
}