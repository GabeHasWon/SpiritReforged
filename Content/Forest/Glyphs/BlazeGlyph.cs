using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Glyphs;

public class BlazeGlyph : GlyphItem
{
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

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				Player.AddBuff(BuffID.OnFire, 120);
				SpawnHitEffects(target.Hitbox.ClosestPointInRect(Player.Center));
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				Player.AddBuff(BuffID.OnFire, 120);
				SpawnHitEffects(proj.Center);
			}
		}

		public void SpawnHitEffects(Vector2 position)
		{
			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];
			ParticleHandler.SpawnParticle(new FireParticle(position, Player.DirectionTo(position), colors, 1, 0.075f, EaseFunction.EaseQuadOut, 40));

			for (int i = 0; i < 4; i++)
			{
				var dust = Dust.NewDustPerfect(position, DustID.Torch, Scale: 1.5f);
				dust.noGravity = Main.rand.NextBool();
				dust.noLightEmittence = true;
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
		c2 = Color.Lerp(Color.Red, Color.OrangeRed, sin);

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
		Color[] emberColors = { 
			Color.Orange,
			Color.Yellow,
			Color.DarkOrange,
			Color.OrangeRed,
			Color.Goldenrod,
			Color.DarkRed,
			Color.Red
		};

		if (Main.rand.NextBool(120))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = Vector2.Zero;

			var particle = new EmberParticle(pos, velocity, Main.rand.Next(emberColors), 1f, 30);
			particle.OverrideDrawLayer(ParticleLayer.AboveItem);
			ParticleHandler.SpawnParticle(particle);
		}

		if (Main.rand.NextBool(15))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(4f), new Color(), 0.05f, EaseFunction.EaseQuadOut, 60, false));

			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];
			ParticleHandler.SpawnParticle(new FireParticle(pos, -Vector2.UnitY * Main.rand.NextFloat(0.5f), colors, 1, Main.rand.NextFloat(0.05f, 0.125f), EaseFunction.EaseQuadOut, 40)
			{
				Layer = ParticleLayer.BelowSolid
			});
		}

		if (Main.rand.NextBool(180))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1f);
			int dir = Main.rand.NextBool() ? -1 : 1;

			ParticleHandler.SpawnParticle(new CurvingEmberParticle(pos, velocity, Main.rand.Next(emberColors), 0.15f, 180, dir, 60)
			{
				rotationalStrength = 0.01f
			});

			ParticleHandler.SpawnParticle(new CurvingEmberParticle(pos, velocity, Color.LightYellow * 0.5f, 0.1f, 180, dir, 60)
			{
				rotationalStrength = 0.01f
			});
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