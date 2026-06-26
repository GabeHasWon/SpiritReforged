using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.MagicPowder;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using static System.Net.Mime.MediaTypeNames;

namespace SpiritReforged.Content.Forest.Glyphs;

public class MoonlightGlyph : GlyphItem
{
	public override void SetStaticDefaults() 
	{ 
		base.SetStaticDefaults();
		GameShaders.Armor.BindShader(Type, new MoonlightGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	} 

	public sealed class MoonlightPlayer : ModPlayer
	{
		// up to 25% more damage at zero mana
		public const float DAMAGE_MULTIPLIER = 0.25f;

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
			{
				modifiers.HideCombatText();

				float strength = 1f - Player.statMana / (float)Player.statManaMax2;
				modifiers.FinalDamage *= 1 + strength * 0.25f; //Deal more damage at zero mana
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
			{
				modifiers.HideCombatText();

				float strength = 1f - Player.statMana / (float)Player.statManaMax2;
				modifiers.FinalDamage *= 1 + strength * DAMAGE_MULTIPLIER; //Deal more damage at zero mana
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
				HitEffects(target, hit, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
				HitEffects(target, hit, damageDone);
		}

		internal void HitEffects(NPC target, NPC.HitInfo hit, int damageDone)
		{
			float strength = MathHelper.Lerp(0.125f, 0.22f, 1f - Player.statMana / (float)Player.statManaMax2);

			float angle = Main.rand.NextFloat(MathHelper.Pi);
			var position = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

			Color c1, c2;
			c1 = Color.Lerp(Color.DarkCyan, Color.MidnightBlue, Main.rand.NextFloat());
			c2 = Color.Lerp(Color.DarkSlateBlue, Color.RoyalBlue, Main.rand.NextFloat());

			var circle = new TexturedPulseCircle(position, (c1 * 0.5f).Additive(), 2, 250 * strength, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
			circle.Angle = angle;
			ParticleHandler.SpawnParticle(circle);

			var circle2 = new TexturedPulseCircle(position, (Color.White * 0.5f).Additive(), 1, 230 * strength, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
			circle2.Angle = angle;
			ParticleHandler.SpawnParticle(circle2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, c2.Additive() * 0.5f, c1.Additive() * 0.5f, 4f * strength, 15, 0.1f));
			ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.White.Additive() * 0.5f, c1.Additive() * 0.5f, 2f * strength, 15));

			for (int i = 0; i < 50 * strength; i++)
			{
				ParticleHandler.SpawnParticle(new MagicParticle(position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextBool() ? c1 : c2, Main.rand.NextFloat(0.5f, 1f), Main.rand.Next(10, 30)));
			}

			if (Player.statMana < Player.statManaMax2)
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/NPCHit/WispHit") with { Pitch = -0.1f, PitchVariance = 0.2f }, target.Center);

				int manaIncrease = (int)Math.Max(damageDone / 5f, 1);

				Player.statMana = Math.Min(Player.statMana + manaIncrease, Player.statManaMax2);

				Player.ManaEffect(manaIncrease); //Leeching

				ParticleHandler.SpawnParticle(new ImpactLine(position + position.DirectionTo(Player.Center) * 20, Vector2.Zero, Color.RoyalBlue.Additive(), new Vector2(0.5f, 1.5f), 20, 0)
				{
					Rotation = position.DirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2,
				});

				ParticleHandler.SpawnParticle(new ImpactLine(position + position.DirectionTo(Player.Center) * 20, Vector2.Zero, Color.White.Additive(), new Vector2(0.5f, 1.5f) * 0.7f, 20, 0)
				{
					Rotation = position.DirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2,
				});

				for (int i = 0; i < 3; i++)
				{
					Vector2 velocity = position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 10f);

					float scale = Main.rand.NextFloat(0.3f, 1f);
					int lifeTime = Main.rand.Next(20, 60);

					ParticleHandler.SpawnParticle(new SharpStarParticle(position, velocity, c2.Additive() * 0.5f, c1.Additive() * 0.5f, scale * strength, lifeTime, 0, DecelerateAction)
					{
						Rotation = 0
					});

					ParticleHandler.SpawnParticle(new SharpStarParticle(position, velocity, Color.White.Additive() * 0.5f, c1.Additive() * 0.5f, scale * 0.9f * strength, lifeTime - 5, 0, DecelerateAction)
					{
						Rotation = 0
					});
				}

				static void DecelerateAction(Particle p)
				{
					p.Velocity *= 0.97f;
					p.Rotation += p.Velocity.Length() * 0.2f;
				}
			}

			Color orange = hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

			CombatText.NewText(target.getRect(), orange, Math.Max((int)(damageDone * 0.8f), 1), hit.Crit);
			int magicDamage = CombatText.NewText(target.getRect(), Color.White, Math.Max((int)(damageDone * 0.2f), 1), hit.Crit);

			ColoredCombatText.AddCombatText(magicDamage, Color.RoyalBlue, Color.DarkSlateBlue);
		}
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			DrawData item = input;
			item.position += offset;
			item.color = Color.Blue.Additive() * 0.1f;
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

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["noiseCrystal"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkCyan, Color.MidnightBlue, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.RoyalBlue, Color.DarkSlateBlue, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.BlueViolet.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);
		
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Blue.Additive() * 0.1f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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
		if (Main.rand.NextBool(90))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.DarkBlue.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.LightCyan.Additive(), 0.15f, 30, 0, AddLight: false)
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
		settings = new(Color.RoyalBlue);
	}

	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && !item.DamageType.CountsAsClass(DamageClass.Magic);

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		item.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(item.DamageType, 0.8f))
			.AddSubClass(new(DamageClass.Magic, 0.2f));

		base.OnApplyGlyph(item, context);
	}
}

public class MoonlightGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
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

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkCyan, Color.MidnightBlue, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.RoyalBlue, Color.DarkSlateBlue, cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.BlueViolet.ToVector4());

		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}