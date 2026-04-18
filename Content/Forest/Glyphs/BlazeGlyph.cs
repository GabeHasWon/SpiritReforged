using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using System.Linq;
using static Terraria.GameContent.Animations.IL_Actions.Sprites;

namespace SpiritReforged.Content.Forest.Glyphs;

public class BlazeGlyph : GlyphItem
{
	/*public sealed class BlazeDebuff : ModBuff
	{

	}*/

	public sealed class BlazePlayer : ModPlayer
	{
		public override void MeleeEffects(Item item, Rectangle hitbox)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>() && Main.rand.NextBool(5))
			{
				var dust = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.Torch);
				dust.noGravity = true;
				dust.fadeIn = 1.1f;
				dust.noLightEmittence = true;
			}
		}
		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				if (!Player.HasBuff(BuffID.OnFire))
					SpawnHitEffects(Player.Center, -MathHelper.PiOver2, 1.5f);

				Player.AddBuff(BuffID.OnFire, 60);
				SpawnHitEffects(target.Hitbox.ClosestPointInRect(Player.Center), target.DirectionTo(Player.Center).ToRotation());
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				if (!Player.HasBuff(BuffID.OnFire))
					SpawnHitEffects(Player.Center, -MathHelper.PiOver2, 1.5f);

				//fireFlashTimer = 120;

				Player.AddBuff(BuffID.OnFire, 60);			
				SpawnHitEffects(proj.Center, proj.DirectionTo(Player.Center).ToRotation());
			}
		}

		public void SpawnHitEffects(Vector2 position, float angle, float scale = 1f)
		{
			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];

			ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.DarkOrange.Additive(), 0.3f * scale, 30, 0)
			{
				Layer = ParticleLayer.BelowNPC,
				Rotation = angle,
				TimeActive = 5
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.LightYellow.Additive() * 0.2f * scale, 0.25f, 25, 0)
			{
				Layer = ParticleLayer.BelowNPC,
				Rotation = angle,
				TimeActive = 5
			});

			for (int i = 0; i < 5; i++)
			{
				var dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(5f, 5f), DustID.Torch, Main.rand.NextVector2Circular(1f, 1f));
				dust.noGravity = !Main.rand.NextBool(5);
				if (dust.noGravity)
					dust.scale = 0.5f;
				else
					dust.fadeIn = 1.1f;
				dust.noLightEmittence = true;

				var particle = new EmberParticle(position, Main.rand.NextVector2Circular(1f, 1f), Color.Orange, Main.rand.Next(colors), Main.rand.NextFloat(0.3f), 40, 5);
				particle.OverrideDrawLayer(ParticleLayer.BelowNPC);
				ParticleHandler.SpawnParticle(particle);

				particle = new EmberParticle(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(3f), Color.Orange, Main.rand.Next(colors), Main.rand.NextFloat(0.3f), 40, 5);
				particle.OverrideDrawLayer(ParticleLayer.BelowNPC);
				ParticleHandler.SpawnParticle(particle);

				if (Main.rand.NextBool(3))
				{
					if (i == 0)
						SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap") with { Volume = 0.15f, PitchVariance = 0.15f }, position);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f), new Color(50, 50, 50, 155) * 0.15f, 0.15f * scale, EaseFunction.EaseQuadOut, 60, false)
					{
						Layer = ParticleLayer.BelowNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f), new Color(50, 50, 50, 155) * 0.2f, 0.1f * scale, EaseFunction.EaseQuadOut, 60, false)
					{
						Layer = ParticleLayer.BelowNPC
					});
				}			

				ParticleHandler.SpawnParticle(new FireParticle(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(3f), colors, 1, Main.rand.NextFloat(0.05f, 0.125f) * scale, EaseFunction.EaseQuadOut, 40)
				{
					Layer = ParticleLayer.BelowNPC
				});
			}
		}
	}

	public override void PreDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale)
	{
		var texWhite = TextureColorCache.ColorSolid(texture, Color.White);

		Effect effect = AssetLoader.LoadedShaders["BlazeGlyphShader"].Value;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		Color c1, c2;
		c1 = Color.Lerp(Color.Yellow, Color.DarkOrange, sin);
		c2 = Color.Lerp(Color.Red, Color.OrangeRed, cos);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.15f);
		effect.Parameters["uColor2"].SetValue(c2.ToVector4() * 0.2f);

		var noise = AssetLoader.LoadedTextures["swirlNoise2"].Value;
		var noise2 = AssetLoader.LoadedTextures["swirlNoise"].Value;
		
		effect.Parameters["uImage1"].SetValue(noise);
		effect.Parameters["uImage2"].SetValue(noise2);
		effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.0015f);
		effect.Parameters["uPixelRes"].SetValue(texture.Size().X);
		effect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.03f, 0.06f, Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly / 2))));

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
		
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 8f) * 4;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.White, rotation, origin, scale, 0f, 0f);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
		
		effect.Parameters["uColor1"].SetValue(c1.Additive().ToVector4() * 0.4f);
		effect.Parameters["uColor2"].SetValue(c2.Additive().ToVector4() * 0.4f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.White, rotation, origin, scale, 0f, 0f);
		}

		spriteBatch.RestartToDefault();
	}

	public override void UpdateGlyphItemInWorld(Item item)
	{
		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		Color c1, c2;
		c1 = Color.Lerp(Color.Yellow, Color.DarkOrange, sin);
		c2 = Color.Lerp(Color.Red, Color.OrangeRed, cos);

		Lighting.AddLight(item.Center, Color.Lerp(c1, c2, sin).ToVector3() / 2);

		Color[] emberColors = { 
			Color.Orange,
			Color.DarkOrange,
			Color.OrangeRed,
			Color.Goldenrod,
		};

		if (Main.rand.NextBool(120))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = Vector2.Zero;

			var particle = new EmberParticle(pos, velocity, Color.Orange, Main.rand.Next(emberColors), 1f, 40);
			particle.OverrideDrawLayer(ParticleLayer.AboveItem);
			ParticleHandler.SpawnParticle(particle);
		}

		if (Main.rand.NextBool(15))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(15, 15, 15, 255) * 0.25f, 0.07f, EaseFunction.EaseQuadOut, 60, false));
			
			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(15, 15, 15, 255) * 0.5f, 0.05f, EaseFunction.EaseQuadOut, 60, false));

			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];
			ParticleHandler.SpawnParticle(new FireParticle(pos, -Vector2.UnitY * Main.rand.NextFloat(0.5f), colors, 1, Main.rand.NextFloat(0.05f, 0.125f), EaseFunction.EaseQuadOut, 40)
			{
				Layer = ParticleLayer.BelowSolid
			});
		}

		if (Main.rand.NextBool(60))
		{
			/*Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1f);
			int dir = Main.rand.NextBool() ? -1 : 1;

			ParticleHandler.SpawnParticle(new CurvingEmberParticle(pos, velocity, Main.rand.Next(emberColors), 0.15f, 180, dir, 60)
			{
				rotationalStrength = 0.01f
			});

			ParticleHandler.SpawnParticle(new CurvingEmberParticle(pos, velocity, Color.LightYellow * 0.5f, 0.1f, 180, dir, 60)
			{
				rotationalStrength = 0.01f
			});*/

			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1.25f, 1.5f);

			var particle = new EmberParticle(pos, velocity, Color.Orange, Main.rand.Next(emberColors), Main.rand.NextFloat(0.3f), 60, 5);
			particle.OverrideDrawLayer(ParticleLayer.BelowProjectile);
			ParticleHandler.SpawnParticle(particle);
		}
	}

	public override void ApplyGlyph(Item item, IApplicationContext context)
	{
		item.damage += (int)Math.Round(item.damage * 0.25f);
		item.crit += 10;

		base.ApplyGlyph(item, context);
	}

	public override void SetDefaults()
	{
		Item.height = Item.width = 28;
		Item.rare = ItemRarityID.Pink;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(233, 143, 26));
	}
}