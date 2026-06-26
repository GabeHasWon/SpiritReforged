using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Forest.Glyphs.Rage;

public class RageGlyph : GlyphItem
{
	public sealed class RagePlayer : ModPlayer
	{
		// what percentage of overflow damage should be stored
		internal const float OVERFLOW_DAMAGE_MULT = 1f;

		internal bool activateOverflow;
		// we need to cache npc life before every hit in case they die (to calculate rage overflow damage)
		// target.life would be always 0 in OnHitNPC
		internal int _npcLifeBeforeDeath;
		internal int _overflowDamage;
		internal int _overflowDecayTimer;

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

			var rageIcon = ModContent.Request<Texture2D>("SpiritReforged/Content/Forest/Glyphs/Rage/RageGlyphAnger").Value;

			if (startSpriteBatch)
				sb.BeginDefault();

			if (projCache.Equals(Main.instance.DrawCacheProjsOverPlayers))
				foreach (Player player in Main.ActivePlayers)
				{
					if (!player.TryGetModPlayer(out RagePlayer ragePlayer) || ragePlayer._overflowDamage <= 0 && ragePlayer._fadeOutTimer <= 0)
						continue;

					float scale = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);

					if (ragePlayer._fadeOutTimer > 0)
						scale = MathHelper.Lerp(scale * 1.5f, scale, EaseFunction.EaseCircularOut.Ease(ragePlayer._fadeOutTimer / 10f));

					if (ragePlayer._fadeInTimer > 0)
						scale = MathHelper.Lerp(scale, scale * 2f, EaseFunction.EaseCircularIn.Ease(ragePlayer._fadeInTimer / 20f));

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
						sb.Draw(rageIcon, ragePlayer.oldPositions[i] - player.velocity * 0.2f - Main.screenPosition, null, color.Additive() * (i / 5f) * fadeOut, 0f, rageIcon.Size() / 2f, scale, 0f, 0f);

					sb.Draw(rageIcon, player.Center + new Vector2(-4 * player.direction, player.gfxOffY - 16) + shake - player.velocity * 0.2f - Main.screenPosition, null, color * fadeOut, 0f, rageIcon.Size() / 2f, scale, 0f, 0f);
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

			if (_overflowDecayTimer > 0)
				_overflowDecayTimer--;
			else if (_overflowDamage > 0)
			{
				_overflowDamage = 0;
				Clear();
			}

			if (oldPositions is null)
			{
				oldPositions = [];

				for (int i = 0; i < 5; i++)
					oldPositions.Add(Player.Center + new Vector2(-4 * Player.direction, Player.gfxOffY - 16));
			}

			oldPositions.Add(Player.Center + new Vector2(-4 * Player.direction, Player.gfxOffY - 16));

			while (oldPositions.Count > 5)
				oldPositions.RemoveAt(0);

			if (!GlyphActive && _overflowDamage > 0)
				Clear();

			if (_overflowDamage > 0)
			{
				float scale = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);

				if (scale > 1.05f && Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(-Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));

					ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
				}
			}
		}

		internal void Clear()
		{
			oldPositions.Clear();
			_fadeOutTimer = 10;
			_overflowDamage = 0;
			activateOverflow = false;
			_npcLifeBeforeDeath = 0;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (GlyphActive)
			{
				_npcLifeBeforeDeath = target.life;
				// don't waste overflow damage if the target was to die regardless
				if (_overflowDamage > 0 && target.life >= _overflowDamage)
				{
					//Main.NewText("Overflow Damage: " + _overflowDamage);

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

					SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Pitch = -0.5f }, target.Center);
					SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);
					SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, target.Center);

					if (Main.myPlayer == Player.whoAmI)
						ScreenshakeHelper.Shake(target.Center, target.DirectionTo(Player.Center), 1, 4, 10);

					for (int i = 0; i < 6; i++)
					{
						Vector2 offset = Main.rand.NextVector2CircularEdge(target.width / 2, target.height / 2);

						Vector2 pos = target.Center + offset;
						Vector2 velocity = offset * Main.rand.NextFloat(0.1f);

						ParticleHandler.SpawnParticle(new ImpactLine(pos, velocity, Color.Red.Additive(), new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.8f, 1.1f), 30));
						ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, velocity, Color.Black, new Vector2(0.5f, 1f) * Main.rand.NextFloat(0.8f, 1.1f), 30), 1);

						ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity.RotatedByRandom(1.5f) * Main.rand.NextFloat(2f), Color.Black * 0.3f, 0.1f, EaseFunction.EaseQuinticOut, 30, false));
					}
				}

				if (target.life <= 0 && _npcLifeBeforeDeath - damageDone < 0)
					// whatever was leftover from the hit, ie negative is what we store as extra damage
					if (activateOverflow)
					{
						_overflowDamage += (int)((_npcLifeBeforeDeath - damageDone) * -1 * OVERFLOW_DAMAGE_MULT);
						_overflowDecayTimer = 600;

						ParticleHandler.SpawnParticle(new LightBurst(target.Center, 0f, Color.Red.Additive(), 0.3f, 25));
						ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, Vector2.Zero, Color.White, Color.Red, 0.4f, 60, 0)
						{
							TimeActive = 30,
							Rotation = 0f
						});

						SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.2f }, target.Center);
						SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

						_fadeInTimer = 20;

						for (int i = 0; i < 4; i++)
						{
							Vector2 pos = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);
							Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);

							ParticleHandler.SpawnParticle(new ImpactLine(pos, velocity, Color.Red.Additive(), new Vector2(0.7f, 1f), 30));
							ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, velocity, Color.Black, new Vector2(0.5f, 1f), 30), 1);
						}

						for (int i = 0; i < 7; i++)
						{
							ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(-Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));

							ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
						}
					}
			}
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		GameShaders.Armor.BindShader(Type, new RageGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(176, 16, 20));
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		float shakeCounter = Math.Max((float)Math.Sin(Main.timeForVisualEffects * 0.025f), 0);
		Vector2 shake = Main.rand.NextVector2Circular(1.25f, 1.25f) * shakeCounter;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset + shake;
			item.color = Color.Red * 0.5f;
			drawInfo.DrawDataCache.Add(item);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			item = input;
			item.position += offset + shake;
			item.color = Color.Red * 0.15f;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			DrawData item = input;
			item.position += offset + shake;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(Type);
			drawInfo.DrawDataCache.Add(item);
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

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.rand.NextBool(100))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(0.5f);

			ParticleHandler.SpawnParticle(new ImpactLine(pos, velocity, Color.Red.Additive(), new Vector2(0.7f, 1f), 30)
			{
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, velocity, Color.Black, new Vector2(0.5f, 1f), 30)
			{
				Layer = ParticleLayer.AboveItem
			}, 3);
		}
	}
}

public class RageGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		var noise = AssetLoader.LoadedTextures["swirlNoise"].Value;
		//var gradient = AssetLoader.LoadedTextures["Glyphs/BaseGlyph_RampTexture"].Value;
		var noiseAlt = AssetLoader.LoadedTextures["swirlNoise"].Value;

		GetEffect.Parameters["uImage1"].SetValue(noise);
		GetEffect.Parameters["uImage2"].SetValue(noiseAlt);
		//effect.Parameters["uImage3"].SetValue(gradient);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.OrangeRed, Color.Red, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.DarkRed, new Color(226, 0, 45), cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.Orange.ToVector4());

		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}
