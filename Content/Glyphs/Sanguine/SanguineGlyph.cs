using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Sanguine;

public partial class SanguineGlyph : GlyphItem
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new SanguineGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.DarkRed);
	}

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		item.damage -= (int)Math.Round(item.damage * 0.2f);
		MoRHelper.OverrideElement(item, MoRHelper.Blood);

		base.OnApplyGlyph(item, context);
	}
	protected override void OnRemoveGlyph(Item item, IApplicationContext context) => MoRHelper.OverrideElement(item, MoRHelper.Blood, -1);
	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.color = Color.DarkRed * 0.5f;
			drawInfo.DrawDataCache.Add(item);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			item = input;
			item.position += offset;
			item.color = Color.DarkRed * 0.15f;
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
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));
		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkRed, Color.Red, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.Black, new Color(200, 25, 100), cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());
		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.DarkRed * 0.5f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.DarkRed * 0.15f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.dedServ)
			return;

		if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.Zero, Color.DarkRed, 0.05f, EaseFunction.EaseQuadOut, 30, false));

			var dust = Dust.NewDustPerfect(pos, DustID.Blood, Main.rand.NextVector2Circular(0.5f, 0.5f), 150, default, 1.25f);
			dust.noGravity = true;
			dust.fadeIn = 3;
		}

		if (Main.rand.NextBool(75))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2CircularEdge(item.width / 3, item.height / 3);

			ParticleHandler.SpawnParticle(new StickyBloodParticle(pos, Vector2.Zero, Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), Main.rand.NextFloat(0.02f, 0.12f)));
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (Main.dedServ)
			return;

		Vector2 normalized = velocity.SafeNormalize(Vector2.One);

		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(position + normalized * item.width, DustID.Blood, normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(5f), 50, default, 1.75f).noGravity = true;

			if (Main.rand.NextBool())
				Dust.NewDustPerfect(position + normalized * item.width, DustID.Blood, normalized.SafeNormalize(Vector2.One).RotatedByRandom(0.4f) * Main.rand.NextFloat(3f), 35, default, 0.85f);
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (!Main.dedServ && Main.rand.NextBool(2 + 1 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), DustID.Blood, -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(4f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;
	}
}

public class SanguineGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));
		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkRed, Color.Red, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.Black, new Color(200, 25, 100), cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());
		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}
