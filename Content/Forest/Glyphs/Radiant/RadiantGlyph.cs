using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Glyphs.Radiant;

public class RadiantGlyph : GlyphItem
{
	public sealed class RadiantPlayer : ModPlayer
	{
		public float radiantCooldown;

		public override void Load()
		{
			On_Main.DrawInfernoRings += DrawHalo;
		}

		private void DrawHalo(On_Main.orig_DrawInfernoRings orig, Main self)
		{
			orig(self);

			var tex = AssetLoader.LoadedTextures["Star"].Value;
			var bloomTex = AssetLoader.LoadedTextures["BloomSoft"].Value;
			var godRay = AssetLoader.LoadedTextures["GodrayCircle"].Value;
			
			var noise = AssetLoader.LoadedTextures["swirlNoise"].Value;

			var spriteBatch = Main.spriteBatch;

			if (!Main.player.Any(p => p.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>()))
				return;

			Effect effect = AssetLoader.LoadedShaders["RadiantGlyphParhelia"].Value;

			effect.Parameters["scale"].SetValue(new Vector2(3f, 1.5f));
			effect.Parameters["scaleTwo"].SetValue(new Vector2(0.85f, 3f));
			
			effect.Parameters["outerStarScale"].SetValue(new Vector2(2.0f, 3.0f));

			effect.Parameters["ringRadius"].SetValue(0.25f);
			effect.Parameters["ringThickness"].SetValue(0.07f);
			effect.Parameters["ringOpacity"].SetValue(0.5f);

			effect.Parameters["uImage1"].SetValue(noise);
			effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0015f);
			
			float scale = 0.33f;
		
			foreach (Player player in Main.player)
			{
				if (player.TryGetModPlayer<RadiantPlayer>(out var mp))
				{
					float lerp = mp.radiantCooldown / (player.HeldItem.useTime * 3f);
					lerp = Math.Min(lerp, 1);

					effect.Parameters["distortionStrength"].SetValue(0.02f * lerp);

					spriteBatch.Draw(bloomTex, player.Top + new Vector2(0f, player.gfxOffY) - Main.screenPosition, null, Color.Goldenrod.Additive() * lerp, 0f, bloomTex.Size() / 2f, scale * 3f, 0f, 0f);

					spriteBatch.End();

					effect.Parameters["uColor"].SetValue(Color.Goldenrod.Additive().ToVector4() * lerp);

					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

					spriteBatch.Draw(tex, player.Top + new Vector2(0f, player.gfxOffY) - Main.screenPosition, null, Color.White.Additive() * lerp, 0f, tex.Size() / 2f, scale, 0f, 0f);

					spriteBatch.End();

					effect.Parameters["uColor"].SetValue(Color.White.Additive().ToVector4() * 0.33f * lerp);

					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
					
					spriteBatch.Draw(tex, player.Top + new Vector2(0f, player.gfxOffY) - Main.screenPosition, null, Color.White.Additive() * lerp, 0f, tex.Size() / 2f, scale, 0f, 0f);

					spriteBatch.End();
					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
				}
			}		
		}

		public override void PreUpdate()
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			{
				if (++radiantCooldown > Player.HeldItem.useTime * 3f)
				{
					int radiantBuff = ModContent.BuffType<DivineStrike>();
					if (!Player.HasBuff(radiantBuff))
					{
						ParticleHandler.SpawnParticle(new StarParticle(Player.Center + new Vector2(0, -10 * Player.gravDir), Vector2.Zero, Color.White, Color.Yellow, 0.2f, 10, 0));
						SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
					}

					Player.AddBuff(radiantBuff, 10);
				}
			}
			else
			{
				radiantCooldown = 0;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Player.HasBuff(ModContent.BuffType<DivineStrike>()))
			{
				modifiers.FinalDamage *= 1.5f;

				SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Volume = 0.4f, Pitch = 0.8f }, target.Center);
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<RadiantEnergy>(), 0, 0, Player.whoAmI, target.whoAmI);

				for (int i = 0; i < 5; i++)
					ParticleHandler.SpawnParticle(new StarParticle(target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat() * 2f, Color.Yellow, Main.rand.NextFloat(0.1f, 0.25f), Main.rand.Next(15, 30), 0.1f));

				Player.ClearBuff(ModContent.BuffType<DivineStrike>());
			}

			radiantCooldown = 0;
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.LightRed;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(234, 167, 51));
	}
}