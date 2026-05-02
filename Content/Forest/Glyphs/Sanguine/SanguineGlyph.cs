using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Misc.Bonsai;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System;
using Terraria.Audio;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Forest.Glyphs.Sanguine;
public class SanguineGlyph : GlyphItem
{
	internal class SanguinePlayer : ModPlayer
	{
		internal bool GlyphActive => Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>();

		internal List<SanguineStack> stacks = new();
		internal int lifestealCooldown;
		internal class SanguineStack
		{
			/// <param name="decayTimer">How long the buff stack lasts, in ticks</param>
			/// <param name="damageBonus">How much bonus damage should be added, 0.05: 5% | Bonus is added to 1f</param>
			public SanguineStack(int decayTimer, float damageBonus)
			{
				timer = decayTimer;
				this.damageBonus = damageBonus;
			}

			public int timer;
			public float damageBonus;
		}

		internal static int[] maxTimeLefts = new int[Main.maxCombatText];

		public override void Load()
		{
			On_CombatText.UpdateCombatText += FadeDamageText;
		}

		private void FadeDamageText(On_CombatText.orig_UpdateCombatText orig)
		{
			orig();

			for (int i = 0; i < Main.maxCombatText; i++)
			{
				CombatText text = Main.combatText[i];
				if (maxTimeLefts[i] > 0)
				{
					if (text.active)
					{
						Color blue, orange;

						blue = text.crit ? Color.DarkRed : Color.Red;
						orange = text.crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

						text.color = Color.Lerp(blue, orange, EaseBuilder.EaseCircularInOut.Ease(1f - text.lifeTime / (float)maxTimeLefts[i]));
					}
					else
					{
						maxTimeLefts[i] = 0;
					}
				}
			}
		}

		public override void ResetEffects()
		{
			stacks ??= new();

			foreach (SanguineStack stack in stacks)
			{
				if (stack.timer > 0)
					stack.timer--;
			}

			stacks.RemoveAll(s => s.timer <= 0);

			if (lifestealCooldown > 0)
				lifestealCooldown--;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (GlyphActive && stacks.Count > 0)
			{
				float damageBonus = 1f;
				foreach (SanguineStack stack in stacks)
					damageBonus += stack.damageBonus;

				modifiers.FinalDamage *= damageBonus;

				modifiers.HideCombatText();
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (GlyphActive)
			{
				bool leechedLife = false;

				if (Player.statLife < Player.statLifeMax2 && target.canGhostHeal && lifestealCooldown <= 0)
				{
					float amountToHeal = (float)damageDone / 10;

					amountToHeal *= MathHelper.Lerp(1f, 3f, 1f - Player.statLife / (float)Player.statLifeMax2);
					if ((int)amountToHeal < 1) // if the healing is too minimal, return to prevent rapid fire weapons healing one health rapidly
						return;

					if (amountToHeal > 10)
						amountToHeal = 10;

					Player.Heal((int)amountToHeal);
					stacks.Add(new SanguineStack(180, 0.03f + damageDone * 0.001f)); // 3% increase, plus 0.1% of the damage dealt, ex: 3% + (10 * 0.001) = 4% boost

					leechedLife = true;
					lifestealCooldown = 20;
				}

				HitVisuals(target, leechedLife);

				if (stacks.Count > 0)
				{
					Color orange = hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

					float damageBonus = 1f;
					foreach (SanguineStack stack in stacks)
						damageBonus += stack.damageBonus;

					int originalDamage = (int)(damageDone / damageBonus);
					int bonusDamage = damageDone - originalDamage;
					if (bonusDamage > 0)
					{
						CombatText.NewText(target.getRect(), orange, originalDamage, hit.Crit);
						int magicDamage = CombatText.NewText(target.getRect(), Color.White, bonusDamage, hit.Crit);

						maxTimeLefts[magicDamage] = Main.combatText[magicDamage]?.lifeTime ?? 10;
					}
					else
						CombatText.NewText(target.getRect(), orange, damageDone, hit.Crit);
				}
			}
		}

		internal void HitVisuals(NPC target, bool leechedLife)
		{
			float angle = Main.rand.NextFloat(MathHelper.Pi);
			var position = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

			Color c1, c2;
			c1 = Color.DarkRed;
			c2 = new Color(200, 25, 100);
			
			
			for (int i = 0; i < 3; i++)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Color.DarkRed * 0.3f, 0.06f, EaseFunction.EaseQuadOut, 30, false));

				Dust dust = Dust.NewDustPerfect(position, DustID.Blood, Main.rand.NextVector2Circular(1.5f, 1.5f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
				dust.noGravity = Main.rand.NextBool();
				dust.fadeIn = 2;

				if (Main.rand.NextBool())
					ParticleHandler.SpawnParticle(new StickyBloodParticle(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.2f));
			}

			if (leechedLife)
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/NPCHit/WispHit") with { Pitch = -0.5f, PitchVariance = 0.2f }, target.Center);
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.3f, PitchVariance = 0.1f }, target.Center);
				
				ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, c1.Additive() * 0.5f, c1.Additive() * 0.5f, 0.6f, 25, 0.1f));
				ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.White.Additive() * 0.5f, c2.Additive() * 0.5f, 0.3f, 25));

				ParticleHandler.SpawnParticle(new ImpactLine(position + position.DirectionTo(Player.Center) * 20, Vector2.Zero, c1.Additive(), new Vector2(0.5f, 1.5f), 20, 0)
				{
					Rotation = position.DirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2,
				});

				ParticleHandler.SpawnParticle(new ImpactLine(position + position.DirectionTo(Player.Center) * 20, Vector2.Zero, c2.Additive(), new Vector2(0.5f, 1.5f) * 0.7f, 20, 0)
				{
					Rotation = position.DirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2,
				});

				for (int i = 0; i < 4; i++)
				{
					Dust dust = Dust.NewDustPerfect(position, DustID.Blood, -Vector2.UnitY * 2f + position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 6f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
					dust.noGravity = Main.rand.NextBool();
					dust.fadeIn = 2;

					ParticleHandler.SpawnParticle(new StickyBloodParticle(position, -Vector2.UnitY * 2f + position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 7f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.1f));
				}
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

		var noise = AssetLoader.LoadedTextures["swirlNoise"].Value;
		//var gradient = AssetLoader.LoadedTextures["Glyphs/BaseGlyph_RampTexture"].Value;
		var noiseAlt = AssetLoader.LoadedTextures["swirlNoise"].Value;

		effect.Parameters["uImage1"].SetValue(noise);
		effect.Parameters["uImage2"].SetValue(noiseAlt);
		//effect.Parameters["uImage3"].SetValue(gradient);
		effect.Parameters["itemSize"].SetValue(texture.Size());

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

			spriteBatch.Draw(texWhite, position + offset, frame, Color.DarkRed * 0.5f, rotation, origin, scale, 0f, 0f);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.DarkRed * 0.15f, rotation, origin, scale, 0f, 0f);
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
		if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.Zero, Color.DarkRed, 0.05f, EaseFunction.EaseQuadOut, 30, false));

			Dust dust = Dust.NewDustPerfect(pos, DustID.Blood, Main.rand.NextVector2Circular(0.5f, 0.5f), 150, default, 1.25f);
			dust.noGravity = true;
			dust.fadeIn = 3;
		}

		if (Main.rand.NextBool(75))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2CircularEdge(item.width / 3, item.height / 3);

			ParticleHandler.SpawnParticle(new StickyBloodParticle(pos, Vector2.Zero, Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), Main.rand.NextFloat(0.02f, 0.12f)));
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
