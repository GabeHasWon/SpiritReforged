using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Content.Glyphs.Shock;

public partial class ShockGlyph : GlyphItem
{
	public sealed class ShockGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public int shockTimer;

		public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
		{
			if (shockTimer > 0)
				shockTimer--;
		}
	}

	public static readonly SoundStyle ElectricSting = new("SpiritReforged/Assets/SFX/Projectile/ElectricSting")
	{
		Volume = 1.5f
	};

	public static readonly SoundStyle ElectricZap = new("SpiritReforged/Assets/SFX/Projectile/ElectricZap")
	{
		Volume = 0.5f
	};

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new ShockGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Yellow);
	}
	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		MoRHelper.OverrideElement(item, MoRHelper.Thunder);

		base.OnApplyGlyph(item, context);
	}
	protected override void OnRemoveGlyph(Item item, IApplicationContext context) => MoRHelper.OverrideElement(item, MoRHelper.Thunder, -1);

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(MathHelper.Lerp(0.03f, 0.3f, (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.02f))));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["ElectricNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size() / 2);

		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.03f));

		effect.Parameters["uColor1"].SetValue(Color.Cyan.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightYellow, Color.CornflowerBlue, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Yellow.Additive().ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(1f);

		var globalItem = item.GetGlobalItem<ShockGlobalItem>();

		Vector2 pos = parameters.Position;
		if (globalItem.shockTimer > 0)
			pos += Main.rand.NextVector2CircularEdge(1.5f, 1.5f) * globalItem.shockTimer / 40f;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.CornflowerBlue.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.Cyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			DrawData item = input;
			item.position += offset;
			item.color = Color.CornflowerBlue.Additive() * 0.1f;
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

	// Summon weapons cannot crit
	// Zealous is a crit-chance only reforge so can be used to check if a weapon can crit (I think)
	public override bool CanApplyGlyph(Item item) 
	{
		// We need to check the sample item because if an item has a glyph applied no prefixes can be applied, thus wrongly returning false here
		Item sampleItem = ContentSamples.ItemsByType[item.type];

		bool prefix = sampleItem.CanApplyPrefix(PrefixID.Zealous);

		return base.CanApplyGlyph(item) && !item.CountsAsClass(DamageClass.Summon) && !item.CountsAsClass(DamageClass.SummonMeleeSpeed) && prefix;
	}
	

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.dedServ)
			return;

		ShockGlobalItem globalItem = item.GetGlobalItem<ShockGlobalItem>();

		if (Main.rand.NextBool(120) && globalItem.shockTimer <= 0)
		{
			SoundEngine.PlaySound(ElectricZap with { Volume = 0.3f }, item.Center);

			globalItem.shockTimer = 40;
			for (int i = 0; i < 5; i++)
			{
				Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
				ParticleHandler.SpawnParticle(new ShockBoltParticle(pos, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(20, 50)));
			}
		}

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			ParticleHandler.SpawnParticle(new ShockBoltParticle(pos, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(20, 50)));
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 normalized = velocity.SafeNormalize(Vector2.One);

		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(position + normalized * item.width, Main.rand.NextBool() ? DustID.Electric : ModContent.DustType<YellowElectricDust>(), normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(9f), 0, default, 0.5f).noGravity = true;
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (Main.rand.NextBool(25 + 20 * projectile.extraUpdates))
			ParticleHandler.SpawnParticle(new ShockBoltParticle(projectile.Center, projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.1f) * Main.rand.NextFloat(15f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.7f), 20 + Main.rand.Next(10, 30)));

		if (Main.rand.NextBool(12 + 10 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), Main.rand.NextBool() ? DustID.Electric : ModContent.DustType<YellowElectricDust>(), -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(12f), 0, default, Main.rand.NextFloat(0.4f, 0.6f)).noGravity = true;
	}

	public class ShockGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
	{
		private Effect GetEffect => shader.Value;

		public override void Apply(Entity entity, DrawData? drawData = null)
		{
			if (!drawData.HasValue)
				return;

			GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
			GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
			GetEffect.Parameters["intensity"].SetValue(MathHelper.Lerp(0.03f, 0.3f, (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.02f))));

			GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
			GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["ElectricNoise"].Value);
			GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size() / 2);

			float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.03f));

			GetEffect.Parameters["uColor1"].SetValue(Color.Cyan.ToVector4() * 0.5f);
			GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightYellow, Color.CornflowerBlue, cos).ToVector4() * 0.5f);
			GetEffect.Parameters["uColor3"].SetValue(Color.Yellow.Additive().ToVector4());

			GetEffect.Parameters["baseDepth"].SetValue(4f);
			GetEffect.Parameters["scale"].SetValue(1f);

			Apply();
		}
	}
}