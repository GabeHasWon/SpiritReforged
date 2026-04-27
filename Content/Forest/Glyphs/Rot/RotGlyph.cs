using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using static System.Net.Mime.MediaTypeNames;
using System;
using Terraria;
using Terraria.Audio;
using System.Linq;

namespace SpiritReforged.Content.Forest.Glyphs.Rot;

public class RotGlyph : GlyphItem
{
	public sealed class RotPlayer : ModPlayer
	{
		public int stacks;
		public int decayTimer;
		public bool DebuffActive => stacks > 0;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RotGlyph>())
			{
				var gnpc = target.GetGlobalNPC<RotGlobalNPC>();

				gnpc.AddStack(1, 180);

				foreach (NPC n in Main.npc)
				{
					if (n != target && n.CanBeChasedBy() && n.Distance(target.Center) < 500f)
					{
						if (n.GetGlobalNPC<RotGlobalNPC>().stacks <= 0)
						{
							for (int i = 0; i < 8; i++)
							{
								var pos = n.Center;

								ParticleHandler.SpawnParticle(new FlyParticle(pos, Main.rand.NextVector2CircularEdge(1f, 1f), 0f, Main.rand.NextFloat(0.7f, 1.1f), 60));

								ParticleHandler.SpawnParticle(new SmokeCloud(pos, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), new Color(87, 94, 1, 255) * 0.2f, 0.03f + Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
								{
									Pixellate = true,
									PixelDivisor = 2,
									Layer = ParticleLayer.AboveNPC
								});

								ParticleHandler.SpawnParticle(new SmokeCloud(pos, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), new Color(131, 124, 1) * 0.15f, 0.02f + Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
								{
									Pixellate = true,
									PixelDivisor = 2,
									Layer = ParticleLayer.AboveNPC
								});

								ParticleHandler.SpawnParticle(new SmokeCloud(pos, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), new Color(169, 158, 38) * 0.25f, 0.01f + Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
								{
									Pixellate = true,
									PixelDivisor = 2,
									Layer = ParticleLayer.AboveNPC
								});
							}
						}

						n.GetGlobalNPC<RotGlobalNPC>().AddStack(1);

						break;
					}					
				}

				var position = target.Hitbox.ClosestPointInRect(Player.Center);
				float angle = Main.rand.NextFloat(MathHelper.Pi);

				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid") with { Volume = 0.05f, PitchVariance = 0.5f}, target.Center);

				for (int i = 0; i < 3; i++)
				{
					ParticleHandler.SpawnParticle(new FlyParticle(position, target.Center.DirectionTo(Player.Center).RotatedByRandom(0.2f)
						* Main.rand.NextFloat(1.5f), 0f, 0.5f, 45));

					ParticleHandler.SpawnParticle(new MaggotParticle(position, target.Center.DirectionTo(Player.Center).RotatedByRandom(0.3f)
						* Main.rand.NextFloat(2.5f) - Vector2.UnitY, Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 20 + Main.rand.Next(20)));

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(2f, 2f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.2f, Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(position, target.DirectionTo(Player.Center).RotatedByRandom(1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.15f, Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(position, target.DirectionTo(Player.Center).RotatedByRandom(1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.25f, Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});
				}			
			}
		}

		public override void ResetEffects()
		{
			// add buff here
			if (decayTimer > 0)
				decayTimer--;
			else if (stacks > 0)
			{
				stacks--;
				decayTimer += 60;
			}
		}

		public override void UpdateBadLifeRegen()
		{
			if (DebuffActive)
			{
				if (Player.lifeRegen > 0)
					Player.lifeRegen = 0;

				Player.lifeRegen -= stacks * 3;
			}
		}

		public override void UpdateEquips()
		{
			if (DebuffActive && Main.rand.NextBool(30 - stacks * 2))
			{
				var position = Player.Center + Main.rand.NextVector2CircularEdge(Player.width / 2, Player.height / 2);

				ParticleHandler.SpawnParticle(new FlyParticle(position, -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f), 0f, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 90)));

				if (Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new MaggotParticle(position, Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 40)
					{
						Layer = ParticleLayer.AbovePlayer
					});
				}

				for (int i = 0; i < 2; i++)
				{
					position = Player.Center + Main.rand.NextVector2CircularEdge(Player.width / 2, Player.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.4f, 0.03f + Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					position = Player.Center + Main.rand.NextVector2Circular(Player.width / 2, Player.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.3f, 0.03f + Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					position = Player.Center + Main.rand.NextVector2Circular(Player.width / 2, Player.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.3f, 0.03f + Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});
				}
			}
		}

		public void AddStack(int stackCount, int decayTime = 180)
		{
			stacks += stackCount;
			decayTimer = decayTime;

			if (stacks > RotGlobalNPC.MAX_STACKS)
				stacks = RotGlobalNPC.MAX_STACKS;
		}
	}

	public sealed class RotGlobalNPC : GlobalNPC
	{
		public const int MAX_STACKS = 10;

		public int stacks;
		public int decayTimer;

		public bool Active => stacks > 0;

		public override bool InstancePerEntity => true;

		public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.CanBeChasedBy();

		public override void ResetEffects(NPC npc)
		{
			if (decayTimer > 0)
				decayTimer--;
			else if (stacks > 0)
			{
				stacks--;
				decayTimer += 60;
			}
		}

		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			if (Active)
			{
				if (npc.lifeRegen > 0)
					npc.lifeRegen = 0;

				npc.lifeRegen -= stacks * 2;
				damage = 1;
			}
		}

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (Active)
				drawColor = Color.Lerp(drawColor, Color.Lerp(drawColor, new Color(241, 255, 16), (float)Math.Abs(Math.Sin(Main.GlobalTimeWrappedHourly * 2f))), stacks / (float)MAX_STACKS);
		}

		public override void AI(NPC npc)
		{
			if (Active && Main.rand.NextBool(30 - stacks * 2) && npc.Opacity > 0)
			{
				var position = npc.Center + Main.rand.NextVector2CircularEdge(npc.width / 2, npc.height / 2);

				ParticleHandler.SpawnParticle(new FlyParticle(position, -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f), 0f, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 90)));
				
				if (Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new MaggotParticle(position, Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 40)
					{
						Layer = ParticleLayer.AboveNPC
					});
				}

				for (int i = 0; i < 2; i++)
				{
					position = npc.Center + Main.rand.NextVector2CircularEdge(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.3f, npc.width * 0.001f + Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					position = npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.2f, npc.width * 0.001f + Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					position = npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.2f, npc.width * 0.001f + Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});
				}
			}
		}

		public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
		{
			if (Active)
				target.GetModPlayer<RotPlayer>().AddStack(1 + stacks / 2);
		}

		public void AddStack(int stackCount, int decayTime = 120)
		{
			stacks += stackCount;
			decayTimer = decayTime;

			if (stacks > MAX_STACKS)
				stacks = MAX_STACKS;
		}
	}

	public override void PreDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale)
	{
		var texWhite = TextureColorCache.ColorSolid(texture, Color.White);

		Effect effect = AssetLoader.LoadedShaders["BlazeGlyphShader"].Value;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		Color c1, c2;
		c1 = Color.Lerp(new Color(66, 64, 0), new Color(87, 94, 0), sin);
		c2 = Color.Lerp(new Color(131, 124, 1), new Color(87, 94, 0), cos);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(c2.ToVector4() * 0.5f);

		var noise = AssetLoader.LoadedTextures["noise"].Value;
		var noise2 = AssetLoader.LoadedTextures["swirlNoise"].Value;

		effect.Parameters["uImage1"].SetValue(noise);
		effect.Parameters["uImage2"].SetValue(noise2);
		effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.00075f);
		effect.Parameters["uPixelRes"].SetValue(texture.Size().X);
		effect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.03f, 0.06f, Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly / 2))));

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 8f) * 4;

			spriteBatch.Draw(texWhite, position + offset, frame, Color.White, rotation, origin, scale, 0f, 0f);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.75f);
		effect.Parameters["uColor2"].SetValue(c2.Additive().ToVector4() * 0.75f);

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

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f);

			ParticleHandler.SpawnParticle(new FlyParticle(pos, velocity, 0f, 1f, 90));
		}

		if (Main.rand.NextBool(30))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(87, 94, 1, 255) * 0.5f, 0.1f, EaseFunction.EaseQuadOut, 60, false)
			{
				Pixellate = true,
				PixelDivisor = 3
			});

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(131, 124, 1, 255) * 0.75f, 0.05f, EaseFunction.EaseQuadOut, 60, false)
			{
				Pixellate = true,
				PixelDivisor = 3
			});
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(220, 198, 57));
	}
}