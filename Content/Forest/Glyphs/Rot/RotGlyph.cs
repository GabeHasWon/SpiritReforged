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

				gnpc.AddStack(1);

				var position = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);
				float angle = Main.rand.NextFloat(MathHelper.Pi);

				Color c1, c2;
				c1 = Color.Lerp(new Color(66, 64, 0), new Color(87, 94, 0), Main.rand.NextFloat());
				c2 = Color.Lerp(new Color(131, 124, 1), new Color(169, 158, 38), Main.rand.NextFloat());

				var circle = new TexturedPulseCircle(position, (c1 * 0.5f).Additive(), 2, 30, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
				circle.Angle = angle;
				ParticleHandler.SpawnParticle(circle);

				var circle2 = new TexturedPulseCircle(position, (Color.YellowGreen * 0.1f).Additive(), 1, 30, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
				circle2.Angle = angle;
				ParticleHandler.SpawnParticle(circle2);

				ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, c2.Additive() * 0.5f, c1.Additive() * 0.5f, 0.3f, 15, 0.1f));
				ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.White.Additive() * 0.5f, c1.Additive() * 0.5f, 0.2f, 15));

				for (int i = 0; i < 3; i++)
				{
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

		}

		public override void UpdateBadLifeRegen()
		{
			if (DebuffActive)
			{
				if (Player.lifeRegen > 0)
					Player.lifeRegen = 0;

				Player.lifeRegen -= stacks;
			}
		}

		public override void UpdateEquips()
		{
			// add buff here
			if (decayTimer > 0)
				decayTimer--;
			else if (stacks > 0)
			{
				stacks--;
				decayTimer += 10;
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
				decayTimer += 30;
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
				drawColor = Color.Lerp(drawColor, Color.DarkOliveGreen, stacks / (float)MAX_STACKS);
		}

		public override void AI(NPC npc)
		{
			if (Active && Main.rand.NextBool(30 - stacks * 2))
			{
				for (int i = 0; i < 2; i++)
				{
					var position = npc.Center + Main.rand.NextVector2CircularEdge(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.3f, Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					position = npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.2f, Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});

					position = npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.2f, Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
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
		c2 = Color.Lerp(new Color(131, 124, 1), new Color(169, 158, 38), cos);

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
		/*if (Main.rand.NextBool(90))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = Vector2.Zero;

			Dust.NewDustPerfect(pos, DustID.CorruptGibs, velocity, 100, default, 1f).noGravity = true;
		}*/

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
		settings = new(new(142, 186, 231));
	}
}