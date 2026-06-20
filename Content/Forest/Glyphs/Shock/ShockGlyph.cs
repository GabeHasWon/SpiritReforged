using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Glyphs.Shock;

public class ShockGlyph : GlyphItem
{
	public sealed class ShockPlayer : ModPlayer
	{
		
	}
	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["ElectricNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size() / 2);

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.LightBlue, Color.Cyan, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightYellow, Color.LightCyan, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Yellow.Additive().ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.2f);

		Vector2 pos = parameters.Position;
		if (shockTimer > 0)
			pos += Main.rand.NextVector2Circular(2.5f, 2.5f) * shockTimer / 20f;
		
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.Cyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.LightCyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	int shockTimer;

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.rand.NextBool(40) && shockTimer <= 0)
			shockTimer = 20;

		if (shockTimer > 0)
			shockTimer--;

		if (Main.rand.NextBool(20))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.9f, 1.1f), Color.Yellow, 0f, Main.rand.NextFloat(0.5f, 1.35f), 10 + Main.rand.Next(20, 40))
			{
				
			});
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Yellow);
	}
}