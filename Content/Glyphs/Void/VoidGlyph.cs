using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Void;

public class VoidGlyph : GlyphItem
{
	public static readonly SoundStyle VoidHit1 = new("SpiritReforged/Assets/SFX/Glyph/VoidGlyphExplode1")
	{
		Volume = 1.25f
	};

	public static readonly SoundStyle VoidHit2 = new("SpiritReforged/Assets/SFX/Glyph/VoidGlyphExplode2")
	{
		Volume = 1.25f
	};

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new VoidGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(225, 63, 255));
	}
	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		MoRHelper.OverrideElement(item, MoRHelper.Shadow);

		base.OnApplyGlyph(item, context);
	}
	protected override void OnRemoveGlyph(Item item, IApplicationContext context) => MoRHelper.OverrideElement(item, MoRHelper.Shadow, -1);

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["noiseCrystal"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

		var main = Color.Lerp(new(225, 63, 255), new(166, 63, 255), sin);
		if (sin > 0.5f)
			main = Color.Lerp(main, Color.Black, sin);

		effect.Parameters["uColor1"].SetValue(main.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(new(255, 63, 230), new(255, 63, 192), cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Black * 0.5f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Violet * 0.25f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);

		if (sin > 0)
			spriteBatch.Draw(whiteTexture, parameters.Position, parameters.Source, Color.Black * 0.5f * sin, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			DrawData item = input;
			item.position += offset;
			item.color = Color.Violet * 0.25f;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.color = Color.Black * 0.5f;
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

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));

		if (Main.rand.NextBool(90) && sin < 0.33f)
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Purple.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.LightPink.Additive(), 0.15f, 30, 0, AddLight: false)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});
		}
		else if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new VoidParticle(pos, Vector2.Zero, Color.Purple.Additive(), 0f, 0.25f, 40));

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos + new Vector2(0, 2), Vector2.Zero, Color.Purple.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos + new Vector2(0, 2), Vector2.Zero, Color.LightPink.Additive(), 0.15f, 30, 0, AddLight: false)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 normalized = velocity.SafeNormalize(Vector2.One);
		Vector2 pos = position + normalized * item.width;

		for (int i = 0; i < 2; i++)
		{
			Vector2 vel = normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(5f);

			ParticleHandler.SpawnParticle(new ImpactLine(pos, vel * 1.5f, Color.Purple * 0.5f, new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 40, 0.95f));

			if (Main.rand.NextBool(3))
				ParticleHandler.SpawnParticle(new VoidParticle(pos, vel, Color.Purple.Additive(), 0f, 0.2f, 65));
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (Main.rand.NextBool(45 + 40 * projectile.extraUpdates))
			ParticleHandler.SpawnParticle(new VoidParticle(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(1.5f), Color.Purple.Additive(), 0f, 0.3f, 65));

		if (Main.rand.NextBool(2 + 1 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), DustID.Granite, -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(4f), 150 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;
	}
}

public class VoidGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["noiseCrystal"].Value);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

		var main = Color.Lerp(new(225, 63, 255), new(166, 63, 255), sin);
		if (sin > 0.5f)
			main = Color.Lerp(main, Color.Black, sin);

		GetEffect.Parameters["uColor1"].SetValue(main.ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(new(255, 63, 230), new(255, 63, 192), cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());

		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}