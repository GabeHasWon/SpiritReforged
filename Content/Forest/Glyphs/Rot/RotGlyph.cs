using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Forest.Glyphs.Rot;

public class RotGlyph : GlyphItem
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		//dummy item id again for shader binding
		GameShaders.Armor.BindShader(ModContent.ItemType<EnchantedStamp>(), new RotGlyphShaderData(AssetLoader.LoadedShaders["BlazeGlyphShader"], "mainPass", 0.5f, false));
		GameShaders.Armor.BindShader(Type, new RotGlyphShaderData(AssetLoader.LoadedShaders["BlazeGlyphShader"], "mainPass", 0.75f, true));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(220, 198, 57));
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 8f) * 4;
			DrawData item = input;
			item.position += offset;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(ModContent.ItemType<EnchantedStamp>());

			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(Type);

			drawInfo.DrawDataCache.Add(item);
		}
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["BlazeGlyphShader"].Value;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		var c1 = Color.Lerp(new Color(66, 64, 0), new Color(87, 94, 0), sin);
		var c2 = Color.Lerp(new Color(131, 124, 1), new Color(87, 94, 0), cos);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(c2.ToVector4() * 0.5f);

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.00075f);
		effect.Parameters["uPixelRes"].SetValue(parameters.Texture.Size().X);
		effect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.03f, 0.06f, Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly / 2))));

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 8f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End(); //Two restarts per item instance?
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.75f);
		effect.Parameters["uColor2"].SetValue(c2.Additive().ToVector4() * 0.75f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.dedServ)
			return;

		if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f);
			ParticleHandler.SpawnParticle(new FlyParticle(pos, velocity, 0f, 1f, 90));
		}

		if (Main.rand.NextBool(30))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));
			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(87, 94, 1, 255) * 0.5f, 0.1f, EaseFunction.EaseQuadOut, 60, false)
			{
				Pixellate = true,
				PixelDivisor = 3
			});

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(131, 124, 1, 255) * 0.75f, 0.05f, EaseFunction.EaseQuadOut, 60, false)
			{
				Pixellate = true,
				PixelDivisor = 3
			});
		}
	}
}

public class RotGlyphShaderData(Asset<Effect> shader, string shaderPass, float colorMod, bool additive) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		var c1 = Color.Lerp(new Color(66, 64, 0), new Color(87, 94, 0), sin);
		var c2 = Color.Lerp(new Color(131, 124, 1), new Color(87, 94, 0), cos);
		if (additive)
			c2 = c2.Additive();

		GetEffect.Parameters["uColor1"].SetValue(c1.ToVector4() * colorMod);
		GetEffect.Parameters["uColor2"].SetValue(c2.ToVector4() * colorMod);

		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.00075f);
		GetEffect.Parameters["uPixelRes"].SetValue(drawData.Value.texture.Size().X);
		GetEffect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.03f, 0.06f, Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly / 2))));

		Apply();
	}
}