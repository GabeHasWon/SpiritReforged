using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Forest.Glyphs.Radiant;

public class RadiantGlyph : GlyphItem
{
	public sealed class RadiantPlayer : ModPlayer
	{
		public float radiantCooldown;

		internal int _flashTimer;
		internal bool _flashed;

		internal float baseScale;
		public override void Load()
		{
			On_Main.DrawCachedProjs += DrawParhelia;
		}

		private void DrawParhelia(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
		{
			orig(self, projCache, startSpriteBatch);

			if (projCache.Equals(Main.instance.DrawCacheProjsBehindNPCs))
			{
				var tex = AssetLoader.LoadedTextures["Star"].Value;
				var bloomTex = AssetLoader.LoadedTextures["BloomSoft"].Value;
				var godRay = AssetLoader.LoadedTextures["GodrayCircle"].Value;

				var noise = AssetLoader.LoadedTextures["swirlNoise"].Value;

				var spriteBatch = Main.spriteBatch;

				if (!Main.player.Any(p => p.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>()))
					return;

				float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly);

				Effect effect = AssetLoader.LoadedShaders["RadiantGlyphParhelia"].Value;

				float pulseScale = 1f * MathHelper.Lerp(1f, 0.85f, Math.Abs(pulse));

				effect.Parameters["scale"].SetValue(new Vector2(3f, 1.5f) * pulseScale);
				effect.Parameters["scaleTwo"].SetValue(new Vector2(0.85f, 3f) * pulseScale);

				pulseScale = 1f * MathHelper.Lerp(1f, 0.7f, Math.Abs(pulse));

				effect.Parameters["outerStarScale"].SetValue(new Vector2(2.0f, 3.0f) * pulseScale);

				effect.Parameters["ringRadius"].SetValue(0.25f);
				effect.Parameters["ringThickness"].SetValue(0.07f);
				effect.Parameters["ringOpacity"].SetValue(0.5f + 0.2f * pulse);

				effect.Parameters["uImage1"].SetValue(noise);
				effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0015f);

				effect.Parameters["pixelRes"].SetValue(0f);

				foreach (Player player in Main.player)
				{
					if (player.TryGetModPlayer<RadiantPlayer>(out var mp))
					{
						if (!player.HasBuff<DivineStrike>())
							return;

						float lerp = 1f - mp._flashTimer / 30f;
						lerp = Math.Min(lerp, 1);

						Vector2 scale = new Vector2(0.2f) + new Vector2(0.02f) * pulse;
						//if (mp._flashTimer > 0)
						//	scale = Vector2.Lerp(scale, new Vector2(0.45f, 0.05f), 1f - lerp);

						Color color = Color.Lerp(Color.DarkOrange, Color.Goldenrod, Math.Abs(pulse)).Additive();

						if (mp._flashTimer > 0)
						{
							color = Color.Lerp(color, Color.Yellow.Additive(), 1f - lerp);
							scale += new Vector2(0.05f) * (1f - lerp);
						}

						effect.Parameters["distortionStrength"].SetValue(0.0075f + 0.05f * (1f - lerp));

						//spriteBatch.Begin(default, default, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

						//spriteBatch.Draw(bloomTex, player.Top + new Vector2(0f, player.gfxOffY) - Main.screenPosition, null, color * (lerp - 0.5f * Math.Abs(pulse)), 0f, bloomTex.Size() / 2f, scale * 2f, 0f, 0f);

						//spriteBatch.End();

						Vector2 pos = player.Top + new Vector2(0f, player.gfxOffY);

						effect.Parameters["uColor"].SetValue(color.ToVector4() * lerp * MathHelper.Lerp(0.7f, 1f, Math.Abs(pulse)));

						if (!startSpriteBatch)
							spriteBatch.End();

						spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

						spriteBatch.Draw(tex, pos - Main.screenPosition, null, Color.White.Additive() * lerp, 0f, tex.Size() / 2f, scale, 0f, 0f);

						spriteBatch.End();

						effect.Parameters["uColor"].SetValue(Color.White.Additive().ToVector4() * 0.5f * lerp * MathHelper.Lerp(0.7f, 1f, Math.Abs(pulse)));

						spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

						spriteBatch.Draw(tex, pos - Main.screenPosition, null, Color.White.Additive() * lerp, 0f, tex.Size() / 2f, scale, 0f, 0f);

						spriteBatch.End();

						if (!startSpriteBatch)
							spriteBatch.BeginDefault();
					}
				}
			}
		}

		public override void PreUpdate()
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			{
				if (_flashTimer > 0)
					_flashTimer--;

				baseScale = 0f;

				float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly);

				Color lightColor = Color.Lerp(Color.Orange, Color.LightGoldenrodYellow, Math.Abs(pulse)).Additive();

				if (_flashTimer > 0)
				{
					lightColor = Color.Lerp(lightColor, Color.White, _flashTimer / 60f);

					float lerp = 1f - _flashTimer / 60f;

					baseScale = MathHelper.Lerp(0.1f, 0.3f, EaseBuilder.EaseCircularInOut.Ease(lerp));
				}
				else if (radiantCooldown > Player.HeldItem.useTime * 3f)
					baseScale = 0.3f;

				Lighting.AddLight(Player.Center, lightColor.ToVector3() * 1.5f * baseScale);

				if (++radiantCooldown > Player.HeldItem.useTime * 3f)
				{
					int radiantBuff = ModContent.BuffType<DivineStrike>();
					if (!Player.HasBuff(radiantBuff))
					{
						//ParticleHandler.SpawnParticle(new StarParticle(Player.Center + new Vector2(0, -10 * Player.gravDir), Vector2.Zero, Color.White, Color.Yellow, 0.2f, 10, 0));
						SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);

						for (int i = 0; i < 5; i++)
						{
							Vector2 pos = Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height);

							Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(0.5f);

							ParticleHandler.SpawnParticle(new SharpStarParticle(pos, velocity, Color.Goldenrod.Additive(), 0.2f, 35, 0)
							{
								Rotation = 0f,
								Layer = ParticleLayer.AbovePlayer
							});

							ParticleHandler.SpawnParticle(new SharpStarParticle(pos, velocity, Color.LightGoldenrodYellow.Additive(), 0.15f, 30, 0)
							{
								Rotation = 0f,
								Layer = ParticleLayer.AbovePlayer
							});
						}

						_flashTimer = 30;
					}

					if (Main.rand.NextBool(180))
					{
						Vector2 pos = Player.Top + Main.rand.NextVector2Circular(50, 10);

						ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Goldenrod.Additive(), 0.2f, 35, 0)
						{
							Rotation = 0f,
							Layer = ParticleLayer.AbovePlayer
						});

						ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.LightGoldenrodYellow.Additive(), 0.15f, 30, 0)
						{
							Rotation = 0f,
							Layer = ParticleLayer.AbovePlayer
						});
					}

					Player.AddBuff(radiantBuff, 10);
				}
			}
			else
			{
				baseScale = 0f;
				radiantCooldown = 0;

				_flashTimer = 0;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HasBuff(ModContent.BuffType<DivineStrike>()))
			{
				float scaleModifier = MathHelper.Lerp(0.75f, 2f, Math.Min(hit.Damage / 200f, 1));

				SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Volume = 0.4f, Pitch = 0.8f }, target.Center);
				//Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<RadiantEnergy>(), 0, 0, Player.whoAmI, target.whoAmI);

				//for (int i = 0; i < 5; i++)
				//	ParticleHandler.SpawnParticle(new StarParticle(target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat() * 2f, Color.Yellow, Main.rand.NextFloat(0.1f, 0.25f), Main.rand.Next(15, 30), 0.1f));

				var glowPos = target.Center;
				var ease = Bomb.EffectEase;
				var stretch = Vector2.One;
				float angle = Main.rand.NextFloat(MathHelper.Pi);

				ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.LightGoldenrodYellow.Additive(), Color.DarkGoldenrod.Additive(), 0.6f, 120 * scaleModifier, 20, "Smoke", stretch, ease)
				{
					Angle = angle
				});

				ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.White.Additive(), Color.DarkGoldenrod.Additive(), 0.3f, 120 * scaleModifier, 20, "Smoke", stretch, ease)
				{
					Angle = angle
				});

				ParticleHandler.SpawnParticle(new LightBurst(glowPos, angle, Color.Goldenrod.Additive(), 0.5f * scaleModifier, 30));
				ParticleHandler.SpawnParticle(new LightBurst(glowPos, angle, Color.White.Additive(), 0.3f * scaleModifier, 25));

				for (int i = 0; i < 2 + 5 * scaleModifier / 2; i++)
				{
					glowPos = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

					Vector2 velocity = Main.rand.NextVector2Circular(2f, 2f) * scaleModifier;
					float scale = Main.rand.NextFloat(0.1f, 0.3f) * scaleModifier;

					ParticleHandler.SpawnParticle(new SharpStarParticle(glowPos, velocity, Color.Goldenrod.Additive(), scale, 35, 0, DecelerateAction)
					{
						Rotation = 0
					});

					ParticleHandler.SpawnParticle(new SharpStarParticle(glowPos, velocity, Color.White.Additive(), scale, 35, 0, DecelerateAction)
					{
						Rotation = 0
					});
				}

				static void DecelerateAction(Particle p)
				{
					p.Velocity *= 0.95f;
					p.Rotation += p.Velocity.Length() * 0.2f;
				}

				Player.ClearBuff(ModContent.BuffType<DivineStrike>());
			}

			radiantCooldown = 0;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Player.HasBuff(ModContent.BuffType<DivineStrike>()))
				modifiers.FinalDamage *= 2.5f;
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.LightRed;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(234, 167, 51));
	}

	public override void PreDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale)
	{
		var texWhite = TextureColorCache.ColorSolid(texture, Color.White);

		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		var noise = AssetLoader.LoadedTextures["vnoise"].Value;
		//var gradient = AssetLoader.LoadedTextures["Glyphs/BaseGlyph_RampTexture"].Value;
		var noiseAlt = AssetLoader.LoadedTextures["vnoise"].Value;

		effect.Parameters["uImage1"].SetValue(noise);
		effect.Parameters["uImage2"].SetValue(noiseAlt);
		//effect.Parameters["uImage3"].SetValue(gradient);
		effect.Parameters["itemSize"].SetValue(texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.Gold, Color.Goldenrod, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.Orange, Color.PaleGoldenrod, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.White.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

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
	}
}