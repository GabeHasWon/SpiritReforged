using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.WorldBuilding;
using static AssGen.Assets;

namespace SpiritReforged.Content.Forest.Glyphs;

public class RadiantGlyph : GlyphItem
{
	public override void SetStaticDefaults() 
	{
		base.SetStaticDefaults();
		GameShaders.Armor.BindShader(Type, new RadiantGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	} 

	public sealed class DivineStrike : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.GetModPlayer<RadiantPlayer>().divineStrike)
			{
				player.buffTime[buffIndex] = 18000;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	public class RadiantItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public int timeInWorld;

		public override void UpdateInventory(Item item, Player player)
		{
			if (timeInWorld > 0)
				timeInWorld = 0;
		}

		public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			{
				if (Main.dayTime)
				{
					if (timeInWorld < 180)
						timeInWorld++;
				}
				else if (timeInWorld > 0)
					timeInWorld -= 3;
			}
		}
	}

	public class RadiantPlayer : ModPlayer
	{
		public float radiantCooldown;
		public bool divineStrike;

		private int _flashTimer;
		private float _baseScale;

		private int dissipateTimer;

		// Two seconds, plus 5% of the items use time.
		public int ChargeTime => (int)(120 + Player.HeldItem.useTime * 0.05f);

		public override void Load() => On_Main.DrawCachedProjs += DrawParhelia;

		private static void DrawParhelia(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
		{
			orig(self, projCache, startSpriteBatch);

			if (projCache.Equals(Main.instance.DrawCacheProjsBehindNPCs))
			{
				var tex = ModContent.Request<Texture2D>("SpiritReforged/Content/Forest/Glyphs/RadiantGlyph_Aura").Value;
				var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
				var star = AssetLoader.LoadedTextures["Star"].Value;
				var spriteBatch = Main.spriteBatch;

				foreach (Player player in Main.ActivePlayers)
				{
					if (!player.TryGetModPlayer(out RadiantPlayer radiantPlayer) || !radiantPlayer.divineStrike && radiantPlayer.dissipateTimer <= 0)
						continue;

					float lerp = 1f - radiantPlayer._flashTimer / 30f;
					lerp = EaseBuilder.EaseCircularOut.Ease(Math.Min(lerp, 1));

					if (radiantPlayer.dissipateTimer > 0)
						lerp = EaseBuilder.EaseCircularIn.Ease(Math.Min(radiantPlayer.dissipateTimer / 20f, 1));

					Vector2 pos = player.Center + new Vector2(-7 * player.direction, player.gfxOffY - 20 * lerp) - player.velocity * 0.5f;

					float scaleFactor = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.05f;

					SpriteEffects flip = SpriteEffects.None;
					if (player.direction == -1)
						flip = SpriteEffects.FlipHorizontally;

					if (!startSpriteBatch)
						spriteBatch.End();

					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
					
					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, new Color(255, 161, 54) * 0.4f * lerp, 0f, bloom.Size() / 2f, 0.6f * scaleFactor, flip, 0f);
					
					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, new Color(255, 212, 87) * 0.35f * lerp, 0f, bloom.Size() / 2f, 0.5f * scaleFactor, flip, 0f);
					
					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, new Color(250, 252, 218) * 0.3f * lerp, 0f, bloom.Size() / 2f, 0.4f * scaleFactor, flip, 0f);

					spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 161, 54) * lerp, 0f, tex.Size() / 2f, 0.8f * scaleFactor, flip, 0f);
					
					spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 212, 87) * 0.4f * lerp, 0f, tex.Size() / 2f, 0.75f * scaleFactor, flip, 0f);
					
					spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(250, 252, 218) * 0.3f * lerp, 0f, tex.Size() / 2f, 0.7f * scaleFactor, flip, 0f);
					
					spriteBatch.Draw(tex, pos - Main.screenPosition, null, Color.White * 0.3f * lerp, 0f, tex.Size() / 2f, 0.6f * scaleFactor, flip, 0f);
					
					spriteBatch.Draw(star, pos - Main.screenPosition, null, new Color(255, 161, 54) * 0.3f * lerp, 0f, star.Size() / 2f, new Vector2(0.45f, 0.225f) * scaleFactor, flip, 0f);

					spriteBatch.Draw(star, pos - Main.screenPosition, null, new Color(255, 212, 87) * lerp, 0f, star.Size() / 2f, new Vector2(0.4f, 0.2f) * scaleFactor, flip, 0f);

					spriteBatch.Draw(star, pos - Main.screenPosition, null, new Color(250, 252, 218) * lerp, 0f, star.Size() / 2f, new Vector2(0.3f, 0.15f) * scaleFactor, flip, 0f);

					spriteBatch.End();

					if (!startSpriteBatch)
						spriteBatch.BeginDefault();
				}
			}
		}

		public override void PreUpdate()
		{
			if (dissipateTimer > 0)
				dissipateTimer--;

			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			{
				if (_flashTimer > 0)
					_flashTimer--;
		
				_baseScale = 0f;

				float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly);
				Color lightColor = Color.Lerp(Color.Orange, Color.LightGoldenrodYellow, Math.Abs(pulse)).Additive();

				if (_flashTimer > 0)
				{
					lightColor = Color.Lerp(lightColor, Color.White, _flashTimer / 60f);
					float lerp = 1f - _flashTimer / 60f;

					_baseScale = MathHelper.Lerp(0.1f, 0.3f, EaseBuilder.EaseCircularInOut.Ease(lerp));
				}
				else if (radiantCooldown > ChargeTime)
				{
					_baseScale = 0.3f;
				}

				Lighting.AddLight(Player.Center, lightColor.ToVector3() * 1.5f * _baseScale);

				if (++radiantCooldown > ChargeTime)
				{
					if (!divineStrike)
					{
						SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);

						for (int i = 0; i < 5; i++)
						{
							Vector2 pos = Player.Center + new Vector2(-7 * Player.direction, 0f) + Main.rand.NextVector2Circular(Player.width, Player.height);
							Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1f);

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

					if (Main.rand.NextBool(90))
					{
						Vector2 top = Player.Top + Main.rand.NextVector2Circular(50, 10);

						ParticleHandler.SpawnParticle(new SharpStarParticle(top, Vector2.Zero, Color.Goldenrod.Additive(), 0.2f, 35, 0, AddLight: false)
						{
							Rotation = 0f,
							Layer = ParticleLayer.AbovePlayer,
						});

						ParticleHandler.SpawnParticle(new SharpStarParticle(top, Vector2.Zero, Color.LightGoldenrodYellow.Additive(), 0.15f, 30, 0, AddLight: false)
						{
							Rotation = 0f,
							Layer = ParticleLayer.AbovePlayer
						});
					}

					if (Main.rand.NextBool(60))
					{
						for (int i = 0; i < 2; i++)
						{
							Vector2 pos = new Vector2(-7, -20);

							float rot = Main.rand.NextFloat(6.28f);
							int dir = Main.rand.NextBool() ? -1 : 1;
							ParticleHandler.SpawnParticle(new LightFlash(Player, pos, Color.LightGoldenrodYellow, new Color(255, 212, 87), new Vector2(0.6f, 0.75f) * Main.rand.NextFloat(0.75f, 1.25f), 60 + Main.rand.Next(10, 30), rot, dir)
							{
								Layer = ParticleLayer.BelowSolid,
								fromRadiant = true
							});

							ParticleHandler.SpawnParticle(new LightFlash(Player, pos, Color.LightYellow, Color.Goldenrod, new Vector2(0.65f, 0.75f) * Main.rand.NextFloat(1f, 1.5f), 30 + Main.rand.Next(10, 30), rot, dir)
							{
								Layer = ParticleLayer.BelowSolid,
								fromRadiant = true
							});
						}					
					}

					divineStrike = true;
					Player.AddBuff(ModContent.BuffType<DivineStrike>(), 60);
				}
			}
			else
			{
				if (divineStrike)
					dissipateTimer = 20;

				divineStrike = false;
				radiantCooldown = 0;
				
				_baseScale = 0f;
				_flashTimer = 0;
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (divineStrike && item.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
				HitEffects(target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (divineStrike && proj.GetGlobalProjectile<GlyphGlobalProjectile>().glyph.ItemType == ModContent.ItemType<RadiantGlyph>())
				HitEffects(target, damageDone);
		}

		public void HitEffects(NPC target, int damageDone)
		{
			float scaleModifier = MathHelper.Lerp(0.75f, 2f, Math.Min(damageDone / 200f, 1));

			SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Volume = 0.4f, Pitch = 0.8f }, target.Center);

			Vector2 glowPos = target.Center;
			PolynomialEase ease = Bomb.EffectEase;
			Vector2 stretch = Vector2.One;
			float angle = Main.rand.NextFloat(MathHelper.Pi);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.LightGoldenrodYellow.Additive(), Color.DarkGoldenrod.Additive(), 0.6f, 120 * scaleModifier, 20, "Smoke", stretch, ease)
			{ Angle = angle });

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.White.Additive(), Color.DarkGoldenrod.Additive(), 0.3f, 120 * scaleModifier, 20, "Smoke", stretch, ease)
			{ Angle = angle });

			ParticleHandler.SpawnParticle(new LightBurst(glowPos, angle, Color.Goldenrod.Additive() * 0.3f, 0.9f * scaleModifier, 60));
			ParticleHandler.SpawnParticle(new LightBurst(glowPos, angle, Color.LightYellow.Additive() * 0.2f, 0.6f * scaleModifier, 45));

			for (int i = 0; i < 2 + 5 * scaleModifier / 2; i++)
			{
				glowPos = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

				Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) * scaleModifier;
				float scale = Main.rand.NextFloat(0.05f, 0.15f) * scaleModifier;

				int timeLeft = Main.rand.Next(20, 40);
				float rot = Main.rand.NextFloat(6.28f);

				ParticleHandler.SpawnParticle(new SharpStarParticle(glowPos, velocity, Color.DarkOrange.Additive(), scale, timeLeft, 0, DecelerateAction)
				{ Rotation = rot });

				ParticleHandler.SpawnParticle(new SharpStarParticle(glowPos, velocity, Color.LightGoldenrodYellow.Additive() * 0.5f, scale, timeLeft, 0, DecelerateAction)
				{ Rotation = rot });
			}

			for (int i = 0; i < 10; i++)
			{
				Vector2 pos = Vector2.Zero;

				float rot = Main.rand.NextFloat(6.28f);
				int dir = Main.rand.NextBool() ? -1 : 1;
				ParticleHandler.SpawnParticle(new LightFlash(target, pos, Color.LightGoldenrodYellow, new Color(255, 212, 87), new Vector2(0.6f, 0.75f) * Main.rand.NextFloat(0.75f, 1.25f) * (scaleModifier * 0.7f), 20 + Main.rand.Next(5, 40), rot, dir)
				{
					Layer = ParticleLayer.BelowSolid,
					fromRadiant = true
				});

				ParticleHandler.SpawnParticle(new LightFlash(target, pos, Color.LightYellow, Color.Goldenrod, new Vector2(0.65f, 0.75f) * Main.rand.NextFloat(1f, 1.5f) * (scaleModifier * 0.7f), 10 + Main.rand.Next(5, 40), rot, dir)
				{
					Layer = ParticleLayer.BelowSolid,
					fromRadiant = true
				});
			}

			dissipateTimer = 20;

			radiantCooldown = 0;
			divineStrike = false;

			static void DecelerateAction(Particle p)
			{
				p.Velocity *= 0.95f;
				p.Rotation += p.Velocity.Length() * 0.2f;
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (divineStrike && item.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
				modifiers.FinalDamage *= 2.5f;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (divineStrike && proj.GetGlobalProjectile<GlyphGlobalProjectile>().glyph.ItemType == ModContent.ItemType<RadiantGlyph>())
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

		var tex = ModContent.Request<Texture2D>("SpiritReforged/Content/Forest/Glyphs/RadiantGlyph_Sun").Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		var star = AssetLoader.LoadedTextures["Star"].Value;

		float lerp = 0f;

		int time = item.GetGlobalItem<RadiantItem>().timeInWorld;
		if (time > 60)
			lerp = EaseBuilder.EaseOutBack().Ease((time - 60) / 120f);
		
		float scaleFactor = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.05f;

		float y = item.Size.Y;

		spriteBatch.Draw(bloom, parameters.Position - item.velocity + new Vector2(0f, -y * lerp), null, Color.DarkOrange.Additive() * lerp * 0.4f, 0f, bloom.Size() / 2f, 0.4f * scaleFactor, 0, 0);
		
		spriteBatch.Draw(bloom, parameters.Position - item.velocity + new Vector2(0f, -y * lerp), null, Color.Orange.Additive() * lerp * 0.35f, 0f, bloom.Size() / 2f, 0.2f * scaleFactor, 0, 0);
		
		spriteBatch.Draw(bloom, parameters.Position - item.velocity + new Vector2(0f, -y * lerp), null, new Color(250, 252, 218, 0) * lerp * 0.3f, 0f, bloom.Size() / 2f, 0.15f * scaleFactor, 0, 0);

		spriteBatch.Draw(tex, parameters.Position - item.velocity + new Vector2(0f, -y * lerp), null, Color.White * lerp, 0f, tex.Size() / 2f, 1f * scaleFactor, 0, 0);

		spriteBatch.Draw(star, parameters.Position - item.velocity + new Vector2(0f, -y * lerp), null, Color.DarkOrange.Additive() * lerp * 0.4f, 0f, star.Size() / 2f, 0.3f * scaleFactor, 0, 0);

	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
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

		if (Main.rand.NextBool(120) && item.GetGlobalItem<RadiantItem>().timeInWorld >= 180)
		{
			Vector2 pos = new Vector2(0f, -item.Size.Y);

			float rot = MathHelper.Pi + Main.rand.NextFloat(-0.3f, 0.3f);
			int dir = Main.rand.NextBool() ? -1 : 1;

			ParticleHandler.SpawnParticle(new LightFlash(item, pos, Color.DarkOrange, new Color(255, 212, 87), new Vector2(1f, 1.25f) * Main.rand.NextFloat(0.75f, 1.25f), 60 + Main.rand.Next(5, 40), rot, dir)
			{
				Layer = ParticleLayer.AboveItem,
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