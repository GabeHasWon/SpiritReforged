using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Radiant;

public class RadiantGlyph : GlyphItem
{
	public sealed class DivineStrike : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.GetModPlayer<RadiantPlayer>().DivineStrike)
				player.buffTime[buffIndex] = 18000;
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		MoRHelper.OverrideElement(item, MoRHelper.Holy);

		base.OnApplyGlyph(item, context);
	}
	protected override void OnRemoveGlyph(Item item, IApplicationContext context) => MoRHelper.OverrideElement(item, MoRHelper.Holy, -1);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new RadiantGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.LightRed;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(234, 167, 51));
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
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

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.LightYellow, Color.Goldenrod, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightGoldenrodYellow, Color.Orange, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.LightGoldenrodYellow.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Goldenrod.Additive() * 0.25f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Gold.Additive() * 0.1f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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

		if (Main.rand.NextBool(180))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Goldenrod.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.LightGoldenrodYellow.Additive(), 0.15f, 30, 0, AddLight: false)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});
		}

		if (Main.rand.NextBool(60))
		{
			var pos = new Vector2(0f, 0f);

			float rot = Main.rand.NextFloat(6.28f);
			int dir = Main.rand.NextBool() ? -1 : 1;

			ParticleHandler.SpawnParticle(new LightFlash(item, pos, Color.DarkOrange, new Color(255, 212, 87), new Vector2(1f, 1.25f) * Main.rand.NextFloat(0.75f, 1.25f), 60 + Main.rand.Next(5, 40), rot, dir)
			{
				Layer = ParticleLayer.BelowProjectile,
				fromRadiant = true
			});
		}
	}
}

public class RadiantGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.LightYellow, Color.Goldenrod, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightGoldenrodYellow, Color.Orange, cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.LightGoldenrodYellow.ToVector4());

		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}