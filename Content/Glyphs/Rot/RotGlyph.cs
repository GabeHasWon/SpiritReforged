using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Rot;

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

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(pos, -Vector2.UnitY * Main.rand.NextFloat(1.5f), new Color(87, 94, 1), 40, false, false, SmokeUpdate)
			{
				Layer = ParticleLayer.BelowNPC
			});

			pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(pos, -Vector2.UnitY * Main.rand.NextFloat(1.5f), new Color(131, 124, 1), 40, false, false, SmokeUpdate)
			{
				Layer = ParticleLayer.BelowNPC
			});

			static void SmokeUpdate(Particle p)
			{
				p.Velocity.Y -= 0.01f;
				p.Velocity *= 0.95f;
			}
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 normalized = velocity.SafeNormalize(Vector2.One);

		for (int i = 0; i < 5; i++)
		{
			Vector2 pos = position + normalized * item.width;
			Vector2 vel = normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f);

			Dust.NewDustPerfect(pos, DustID.Poisoned, vel, 100, default, 1.5f).noGravity = true;

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(pos, vel, new Color(131, 124, 1), 35, false, false, SmokeUpdate)
			{
				Layer = ParticleLayer.BelowNPC
			});

			static void SmokeUpdate(Particle p)
			{
				p.Velocity *= 0.95f;
			}
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (Main.rand.NextBool(4 + 3 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), DustID.Poisoned, -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(4f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;

		if (Main.rand.NextBool(30 + 25 * projectile.extraUpdates))
			ParticleHandler.SpawnParticle(new FlyParticle(projectile.Center, Main.rand.NextVector2Circular(1.5f, 1.5f), 0f, Main.rand.NextFloat(0.8f, 1.2f), 40));

		if (Main.rand.NextBool(3 + 2 * projectile.extraUpdates))
		{
			Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2);

			Vector2 vel = projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 4f) + Main.rand.NextVector2Circular(0.5f, 0.5f);

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(pos, vel, new Color(169, 158, 38), 20, false, false, SmokeUpdate)
			{
				Layer = ParticleLayer.BelowNPC
			});

			static void SmokeUpdate(Particle p)
			{
				p.Velocity *= 0.95f;
			}
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