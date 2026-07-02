using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using Terraria.Audio;
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
			if (player.GetModPlayer<RadiantPlayer>().divineStrike)
				player.buffTime[buffIndex] = 18000;
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	public sealed class RadiantGlobalItem : GlobalItem
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
				if (Main.dayTime)
					if (timeInWorld < 180)
						timeInWorld++;
					else if (timeInWorld > 0)
						timeInWorld -= 3;
		}
	}

	public sealed class RadiantPlayer : ModPlayer
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
				var tex = ModContent.Request<Texture2D>("SpiritReforged/Content/Forest/Glyphs/Radiant/RadiantGlyph_Aura2").Value;
				var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
				var star = AssetLoader.LoadedTextures["Star"].Value;
				var spriteBatch = Main.spriteBatch;

				foreach (Player player in Main.ActivePlayers)
				{
					if (!player.TryGetModPlayer(out RadiantPlayer radiantPlayer) || !radiantPlayer.divineStrike && radiantPlayer.dissipateTimer <= 0)
						continue;

					float lerp = 1f - radiantPlayer._flashTimer / 30f;
					lerp = EaseFunction.EaseCircularOut.Ease(Math.Min(lerp, 1));

					if (radiantPlayer.dissipateTimer > 0)
						lerp = EaseFunction.EaseCircularIn.Ease(Math.Min(radiantPlayer.dissipateTimer / 20f, 1));

					Vector2 pos = player.Center + new Vector2(-9 * player.direction, player.gfxOffY - 25 * lerp) - player.velocity * 0.5f;

					float scaleFactor = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.05f;

					SpriteEffects flip = SpriteEffects.None;
					if (player.direction == -1)
						flip = SpriteEffects.FlipHorizontally;

					if (!startSpriteBatch)
						spriteBatch.End();

					/*Color[] sunColors = [
						new Color(255, 161, 54),
						new Color(255, 212, 87),
						new Color(250, 252, 218),
					];*/

					Color[] sunColors = [
						new Color(255, 150, 50),
						new Color(255, 200, 101),
						new Color(255, 220, 218),
					];

					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, sunColors[0] * 0.4f * lerp, 0f, bloom.Size() / 2f, 0.6f * scaleFactor, flip, 0f);

					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, sunColors[1] * 0.35f * lerp, 0f, bloom.Size() / 2f, 0.5f * scaleFactor, flip, 0f);

					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, sunColors[2] * 0.3f * lerp, 0f, bloom.Size() / 2f, 0.4f * scaleFactor, flip, 0f);

					spriteBatch.Draw(tex, pos - Main.screenPosition, null, sunColors[0] * lerp, 0f, tex.Size() / 2f, 0.8f * scaleFactor, flip, 0f);

					spriteBatch.Draw(tex, pos - Main.screenPosition, null, sunColors[1] * 0.4f * lerp, 0f, tex.Size() / 2f, 0.75f * scaleFactor, flip, 0f);

					spriteBatch.Draw(tex, pos - Main.screenPosition, null, sunColors[2] * 0.3f * lerp, 0f, tex.Size() / 2f, 0.7f * scaleFactor, flip, 0f);

					spriteBatch.Draw(tex, pos - Main.screenPosition, null, Color.White * 0.3f * lerp, 0f, tex.Size() / 2f, 0.6f * scaleFactor, flip, 0f);

					spriteBatch.Draw(star, pos - Main.screenPosition, null, sunColors[0] * 0.3f * lerp, 0f, star.Size() / 2f, new Vector2(0.45f, 0.225f) * scaleFactor, flip, 0f);

					spriteBatch.Draw(star, pos - Main.screenPosition, null, sunColors[1] * lerp, 0f, star.Size() / 2f, new Vector2(0.4f, 0.2f) * scaleFactor, flip, 0f);

					spriteBatch.Draw(star, pos - Main.screenPosition, null, sunColors[2] * lerp, 0f, star.Size() / 2f, new Vector2(0.3f, 0.15f) * scaleFactor, flip, 0f);

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

				if (divineStrike || dissipateTimer > 0)
				{
					float lerp = 1f - _flashTimer / 30f;
					lerp = EaseFunction.EaseCircularOut.Ease(Math.Min(lerp, 1));

					if (dissipateTimer > 0)
						lerp = EaseFunction.EaseCircularIn.Ease(Math.Min(dissipateTimer / 20f, 1));

					Lighting.AddLight(Player.Center, Color.LightGoldenrodYellow.ToVector3() * 0.5f * lerp);
				}

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

					if (Main.rand.NextBool(60))
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

					if (Main.rand.NextBool(35))
					{
						var pos = new Vector2(-9, -25);

						float rot = Main.rand.NextFloat(6.28f);
						int dir = Main.rand.NextBool() ? -1 : 1;
						ParticleHandler.SpawnParticle(new LightFlash(Player, pos, Color.LightGoldenrodYellow, new Color(255, 212, 87), new Vector2(0.6f, 0.75f) * Main.rand.NextFloat(0.5f, 1f), 60 + Main.rand.Next(10, 30), rot, dir)
						{
							Layer = ParticleLayer.BelowSolid,
							fromRadiant = true
						});

						ParticleHandler.SpawnParticle(new LightFlash(Player, pos, Color.LightYellow, Color.Goldenrod, new Vector2(0.65f, 0.75f) * Main.rand.NextFloat(0.7f, 1.15f), 30 + Main.rand.Next(10, 30), rot, dir)
						{
							Layer = ParticleLayer.BelowSolid,
							fromRadiant = true
						});
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

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
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