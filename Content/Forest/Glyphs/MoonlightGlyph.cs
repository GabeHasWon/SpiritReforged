using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.MagicPowder;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Glyphs;

public class MoonlightGlyph : GlyphItem
{
	public sealed class MoonlightPlayer : ModPlayer
	{
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
			{
				float strength = 1f - Player.statMana / (float)Player.statManaMax2;
				damage *= 1 + strength * 0.25f; //Deal 25% more damage at zero mana
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
				modifiers.HideCombatText();
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
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

				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.Blue, new Vector2(1f, 0.5f), 60, 0)
				{
					Rotation = position.DirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2,
				});

				if (Player.statMana < Player.statManaMax2)
				{
					int manaIncrease = (int)Math.Max(damageDone / 20f, 1);

					Player.statMana = Math.Min(Player.statMana + manaIncrease, Player.statManaMax2);

					Player.ManaEffect(manaIncrease); //Leeching
				}

				CombatText.NewText(target.getRect(), hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile, (int)(damageDone * 0.8f), hit.Crit);
				CombatText.NewText(target.getRect(), hit.Crit ? Color.DarkSlateBlue : Color.RoyalBlue, (int)(damageDone * 0.2f), hit.Crit);
			}
		}
	}

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

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkCyan, Color.MidnightBlue, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.RoyalBlue, Color.DarkSlateBlue, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.BlueViolet.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);
		
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.Blue.Additive() * 0.1f, rotation, origin, scale, 0f, 0f);
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

	public override void PostDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale)
	{

	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.RoyalBlue);
	}

	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && !item.DamageType.CountsAsClass(DamageClass.Magic);

	public override void ApplyGlyph(Item item, IApplicationContext context)
	{
		item.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(item.DamageType, 0.8f))
			.AddSubClass(new(DamageClass.Magic, 0.2f));

		base.ApplyGlyph(item, context);
	}
}