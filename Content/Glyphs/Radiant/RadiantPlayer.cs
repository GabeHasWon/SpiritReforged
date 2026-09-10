using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.Silk;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Glyphs.Radiant;

public class RadiantPlayer : ModPlayer
{
	public static readonly Asset<Texture2D> Aura2 = DrawHelpers.RequestLocal<RadiantPlayer>("RadiantGlyph_Aura2", false);
	private const int EASE_MAX = 30;

	public bool DivineStrike => radiantCounter >= ChargeTime;

	public int ChargeTime => (int)(180 + Player.HeldItem.useTime * 0.06f);

	public int radiantCounter;
	private float _ease;

	public override void Load() => On_Main.DrawCachedProjs += DrawParhelia;

	private static void DrawParhelia(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
	{
		orig(self, projCache, startSpriteBatch);

		if (projCache.Equals(Main.instance.DrawCacheProjsBehindNPCs))
		{
			List<RadiantPlayer> queued = [];

			foreach (Player player in Main.ActivePlayers)
			{
				if (player.TryGetModPlayer(out RadiantPlayer radiantPlayer) && radiantPlayer._ease != 0)
					queued.Add(radiantPlayer);
			}

			if (queued.Count > 0)
			{
				Texture2D aura = Aura2.Value;
				Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
				Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
				SpriteBatch spriteBatch = Main.spriteBatch;

				if (!startSpriteBatch)
					spriteBatch.End();

				spriteBatch.Begin(SpriteSortMode.Deferred, AfterimagePlayer.AdditiveNoAlpha, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

				foreach (RadiantPlayer radiantPlayer in queued)
				{
					Player player = radiantPlayer.Player;
					float lerp = EaseFunction.EaseCircularOut.Ease(radiantPlayer._ease / EASE_MAX);
					Vector2 pos = player.Center + new Vector2(-9 * player.direction, player.gfxOffY - 25 * lerp) - player.velocity * 0.5f;
					float scaleFactor = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.05f;

					SpriteEffects flip = (player.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
					if (player.direction == -1)
						flip = SpriteEffects.FlipHorizontally;

					Color[] sunColors =
					[
						new Color(255, 150, 50),
						new Color(255, 200, 101),
						new Color(255, 220, 218),
					];

					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, sunColors[0] * 0.4f * lerp, 0f, bloom.Size() / 2f, 0.6f * scaleFactor, flip, 0f);
					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, sunColors[1] * 0.35f * lerp, 0f, bloom.Size() / 2f, 0.5f * scaleFactor, flip, 0f);
					spriteBatch.Draw(bloom, pos - Main.screenPosition, null, sunColors[2] * 0.3f * lerp, 0f, bloom.Size() / 2f, 0.4f * scaleFactor, flip, 0f);

					spriteBatch.Draw(aura, pos - Main.screenPosition, null, sunColors[0] * lerp, 0f, aura.Size() / 2f, 0.8f * scaleFactor, flip, 0f);
					spriteBatch.Draw(aura, pos - Main.screenPosition, null, sunColors[1] * 0.4f * lerp, 0f, aura.Size() / 2f, 0.75f * scaleFactor, flip, 0f);
					spriteBatch.Draw(aura, pos - Main.screenPosition, null, sunColors[2] * 0.3f * lerp, 0f, aura.Size() / 2f, 0.7f * scaleFactor, flip, 0f);
					spriteBatch.Draw(aura, pos - Main.screenPosition, null, Color.White * 0.3f * lerp, 0f, aura.Size() / 2f, 0.6f * scaleFactor, flip, 0f);

					spriteBatch.Draw(star, pos - Main.screenPosition, null, sunColors[0] * 0.3f * lerp, 0f, star.Size() / 2f, new Vector2(0.45f, 0.225f) * scaleFactor, flip, 0f);
					spriteBatch.Draw(star, pos - Main.screenPosition, null, sunColors[1] * lerp, 0f, star.Size() / 2f, new Vector2(0.4f, 0.2f) * scaleFactor, flip, 0f);
					spriteBatch.Draw(star, pos - Main.screenPosition, null, sunColors[2] * lerp, 0f, star.Size() / 2f, new Vector2(0.3f, 0.15f) * scaleFactor, flip, 0f);
				}

				spriteBatch.End();

				if (!startSpriteBatch)
					spriteBatch.BeginDefault();
			}
		}
	}

	public override void PreUpdate()
	{
		if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>() || Main.projectile.Any(p => p.active && p.owner == Player.whoAmI && Main.projPet[p.type] && p.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>()))
		{
			if (!Main.dedServ)
			{
				if (DivineStrike || _ease > 0)
				{
					float lerp = EaseFunction.EaseCircularOut.Ease(_ease / (float)EASE_MAX);
					Lighting.AddLight(Player.Center, Color.LightGoldenrodYellow.ToVector3() * 0.5f * lerp);
				}
			}

			bool hadDivineStrike = DivineStrike;
			if (++radiantCounter >= ChargeTime)
			{
				if (!Main.dedServ)
				{
					if (!hadDivineStrike)
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
				}

				_ease = Math.Min(_ease + 1, EASE_MAX);
				Player.AddBuff(ModContent.BuffType<RadiantGlyph.DivineStrike>(), 60);
			}
			else
			{
				_ease = Math.Max(_ease - 1, 0);
			}
		}
		else
		{
			radiantCounter = 0;
			_ease = Math.Max(_ease - 1, 0);
		}
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (item.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
		{
			if (DivineStrike)
			{
				RadiantHitEffects(target, Player, damageDone, hit.Crit);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(RadiantHitEffects), -1, -1, target, Player, damageDone, hit.Crit);
			}
			else
				radiantCounter = 0;	
		}
	}

	public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (proj.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
		{
			if (DivineStrike)
			{
				RadiantHitEffects(target, Player, damageDone, hit.Crit);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(RadiantHitEffects), -1, -1, target, Player, damageDone, hit.Crit);
			}
			else
				radiantCounter = 0;
		}
	}

	[NetSynced(true)]
	public static void RadiantHitEffects(NPC target, Player owner, int damageDone, bool crit)
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

		if (owner.TryGetModPlayer(out RadiantPlayer radiantPlayer))
		{
			radiantPlayer.radiantCounter = 0;
			if (crit) // 4 second delay when a crit occurs
				radiantPlayer.radiantCounter -= 240;
		}

		static void DecelerateAction(Particle p)
		{
			p.Velocity *= 0.95f;
			p.Rotation += p.Velocity.Length() * 0.2f;
		}
	}

	public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (DivineStrike && item.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			modifiers.FinalDamage *= 2.5f;
	}

	public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (DivineStrike && proj.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			modifiers.FinalDamage *= 2.5f;
	}
}