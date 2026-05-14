using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs.Sanguine;
using SpiritReforged.Content.Particles;
using System;
using Terraria;
using static SpiritReforged.Content.Forest.Glyphs.RadiantGlyph;

namespace SpiritReforged.Content.Forest.Glyphs;
public class RageGlyph : GlyphItem
{
	public sealed class RagePlayer : ModPlayer
	{
		internal bool activateOverflow;
		// we need to cache npc life before every hit in case they die (to calculate rage overflow damage)
		// target.life would be always 0 in OnHitNPC
		internal int _npcLifeBeforeDeath;
		internal int _overflowDamage;

		// drawing
		internal int _fadeOutTimer;
		internal int _fadeInTimer;
		internal List<Vector2> oldPositions;
		internal bool GlyphActive => Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RageGlyph>();
		public override void Load() => On_Main.DrawCachedProjs += DrawRage;

		private static void DrawRage(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
		{
			orig(self, projCache, startSpriteBatch);

			SpriteBatch sb = Main.spriteBatch;

			var rageIcon = ModContent.Request<Texture2D>("SpiritReforged/Content/Forest/Glyphs/RageGlyphAnger").Value;

			if (startSpriteBatch)
				sb.BeginDefault();

			if (projCache.Equals(Main.instance.DrawCacheProjsOverPlayers))
			{
				foreach (Player player in Main.ActivePlayers)
				{
					if (!player.TryGetModPlayer(out RagePlayer ragePlayer) || ragePlayer._overflowDamage <= 0 && ragePlayer._fadeOutTimer <= 0)
						continue;

					float scale = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);

					if (ragePlayer._fadeOutTimer > 0)
						scale = MathHelper.Lerp(scale * 1.5f, scale, EaseBuilder.EaseCircularOut.Ease(ragePlayer._fadeOutTimer / 10f));

					if (ragePlayer._fadeInTimer > 0)
						scale = MathHelper.Lerp(scale, scale * 2f, EaseBuilder.EaseCircularIn.Ease(ragePlayer._fadeInTimer / 20f));

					Vector2 shake = Main.rand.NextVector2Circular(0.5f, 0.5f) * scale;
					if (scale < 1f || ragePlayer._fadeOutTimer > 0)
						shake *= 0;

					float fadeOut = 1f;

					if (ragePlayer._fadeOutTimer > 0)
						fadeOut = ragePlayer._fadeOutTimer / 10f;

					if (ragePlayer._fadeInTimer > 0)
						fadeOut = 1f - ragePlayer._fadeInTimer / 20f;

					Color color = Color.White;

					if (ragePlayer._fadeOutTimer > 0)
						color = Color.Lerp(color, Color.Orange, ragePlayer._fadeOutTimer / 10f);

					if (ragePlayer._fadeInTimer > 0)
						color = Color.Lerp(color, Color.Orange, ragePlayer._fadeInTimer / 20f);

					for (int i = 0; i < ragePlayer.oldPositions.Count; i++)
					{
						sb.Draw(rageIcon, ragePlayer.oldPositions[i] - Main.screenPosition, null, color.Additive() * (i / 5f) * fadeOut, 0f, rageIcon.Size() / 2f, scale, 0f, 0f);
					}

					sb.Draw(rageIcon, player.Center + new Vector2(-4 * player.direction, player.gfxOffY - 16) + shake - Main.screenPosition, null, color * fadeOut, 0f, rageIcon.Size() / 2f, scale, 0f, 0f);
				}
			}

			if (startSpriteBatch)
				sb.End();
		}

		public override void UpdateEquips()
		{
			if (_fadeOutTimer > 0)
				_fadeOutTimer--;

			if (_fadeInTimer > 0)
				_fadeInTimer--;

			if (oldPositions is null)
			{
				oldPositions = [];

				for (int i = 0; i < 5; i++)
				{
					oldPositions.Add(Player.Center + new Vector2(-4 * Player.direction, Player.gfxOffY - 16));
				}
			}

			oldPositions.Add(Player.Center + new Vector2(-4 * Player.direction, Player.gfxOffY - 16));

			while (oldPositions.Count > 5)
				oldPositions.RemoveAt(0);

			if (!GlyphActive && _overflowDamage > 0)
			{
				oldPositions.Clear();
				_fadeOutTimer = 10;
				_overflowDamage = 0;
				activateOverflow = false;
				_npcLifeBeforeDeath = 0;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (GlyphActive)
			{
				_npcLifeBeforeDeath = target.life;
				// don't waste overflow damage if the target was to die regardless
				if (_overflowDamage > 0 && target.life >= _overflowDamage)
				{
					Main.NewText("Overflow Damage: " + _overflowDamage);

					// dont give the player more overflow damage if were using overflow damage
					activateOverflow = false;	
					
					modifiers.FlatBonusDamage += _overflowDamage;

					modifiers.HideCombatText();
				}		
				else
					activateOverflow = true;
			}			
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (GlyphActive)
			{
				if (!activateOverflow)
				{
					Color orange = hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

					CombatText.NewText(target.getRect(), orange, Math.Max(damageDone - _overflowDamage, 1), hit.Crit);
					int overflow = CombatText.NewText(target.getRect(), Color.White, Math.Max(_overflowDamage, 1), hit.Crit);

					ColoredCombatText.AddCombatText(overflow, Color.Red, Color.DarkRed);

					_fadeOutTimer = 10;
					_overflowDamage = 0;
				}

				if (target.life <= 0 && _npcLifeBeforeDeath - damageDone < 0)
				{
					// whatever was leftover from the hit, ie negative is what we store as extra damage
					if (activateOverflow)
					{
						_overflowDamage += (_npcLifeBeforeDeath - damageDone) * -1;

						ParticleHandler.SpawnParticle(new LightBurst(target.Center, 0f, Color.Red.Additive(), 0.3f, 25));
						ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, Vector2.Zero, Color.White, Color.Red, 0.4f, 60, 0)
						{
							TimeActive = 30,
							Rotation = 0f
						});

						_fadeInTimer = 20;
					}					
				}
			}
		}
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D texWhite = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
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
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.OrangeRed, Color.Red, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.DarkRed, new Color(226, 0, 45), cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Orange.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		float shakeCounter = Math.Max((float)Math.Sin(Main.timeForVisualEffects * 0.025f), 0);
		Vector2 shake = Main.rand.NextVector2Circular(1.25f, 1.25f) * shakeCounter;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(texWhite, parameters.Position + offset + shake, parameters.Source, Color.Red * 0.5f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			spriteBatch.Draw(texWhite, parameters.Position + offset + shake, parameters.Source, Color.Red * 0.15f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(texWhite, parameters.Position + offset + shake, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void UpdateInWorld(Item item)
	{
		if (Main.rand.NextBool(120))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.DarkRed.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.LightPink.Additive(), 0.15f, 30, 0, AddLight: false)
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
		settings = new(Color.Red);
	}
}
