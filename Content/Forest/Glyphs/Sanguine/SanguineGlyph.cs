using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Glyphs.Sanguine;
public class SanguineGlyph : GlyphItem
{
	public override void PreDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale)
	{
		var texWhite = TextureColorCache.ColorSolid(texture, Color.White);

		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		var noise = AssetLoader.LoadedTextures["swirlNoise2"].Value;
		//var gradient = AssetLoader.LoadedTextures["Glyphs/BaseGlyph_RampTexture"].Value;
		var noiseAlt = AssetLoader.LoadedTextures["noiseCrystal"].Value;

		effect.Parameters["uImage1"].SetValue(noise);
		effect.Parameters["uImage2"].SetValue(noiseAlt);
		//effect.Parameters["uImage3"].SetValue(gradient);
		effect.Parameters["itemSize"].SetValue(texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkRed, Color.Red, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.OrangeRed, Color.Red, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.DarkOrange.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.DarkRed * 0.1f, rotation, origin, scale, 0f, 0f);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.White, rotation, origin, scale, 0f, 0f);
		}

		spriteBatch.RestartToDefault();
	}

	public override void UpdateGlyphItemInWorld(Item item)
	{
		if (Main.rand.NextBool(90))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Red.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.MediumVioletRed.Additive() * 0.25f, 0.15f, 30, 0, AddLight: false)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Red);
	}

	public override void ApplyGlyph(Item item, IApplicationContext context)
	{
		item.damage -= (int)Math.Round(item.damage * 0.2f);

		base.ApplyGlyph(item, context);
	}
}
