using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Content.Forest.Glyphs.Rot;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using static AssGen.Assets;
using static SpiritReforged.Content.Ziggurat.Windshear.WindshearScepter;

namespace SpiritReforged.Content.Forest.Glyphs.Storm;

public class StormGlyph : GlyphItem
{
	/// this is from windshear scepter with minor changes to color
	[Autoload(Side = ModSide.Client)]
	public sealed class StormMetaballSystem : ModSystem
	{
		private static readonly ModTarget2D StormTarget = new(static () => StormParticleRenderer.Particles.Count != 0 || Data.Count != 0, DrawCloudTarget);
		private static readonly ParticleRenderer StormParticleRenderer = new();
		private static readonly HashSet<DrawData> Data = [];

		public static void Add(ABasicParticle particle) => StormParticleRenderer.Add(particle);
		public static void Add(DrawData drawData) => Data.Add(drawData);

		public override void Load() => On_Main.DrawProjectiles += DrawShader;

		private static void DrawShader(On_Main.orig_DrawProjectiles orig, Main self)
		{
			if (StormTarget != null && StormTarget.Active)
			{
				Effect s = AssetLoader.LoadedShaders["CloudMetaball"].Value;
				SpriteBatch spriteBatch = Main.spriteBatch;

				s.Parameters["primaryColor"].SetValue(Color.LightCyan.ToVector4());
				s.Parameters["secondaryColor"].SetValue(new Color(50, 50, 50, 150).ToVector4());
				s.Parameters["numColors"].SetValue(3);
				ShaderHelpers.SetEffectMatrices(ref s);

				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

				spriteBatch.Draw(StormTarget, Vector2.Zero, Color.White);

				spriteBatch.End();
			}

			orig(self);
		}

		private static void DrawCloudTarget(SpriteBatch spriteBatch)
		{
			StormParticleRenderer.Settings.AnchorPosition = -Main.screenPosition;
			StormParticleRenderer.Draw(spriteBatch);

			foreach (DrawData data in Data)
				spriteBatch.Draw(data.texture, data.position, data.sourceRect, data.color, data.rotation, data.origin, data.scale, data.effect, 0);

			Data.Clear();
		}

		public override void PostUpdateProjectiles() => StormParticleRenderer.Update();
	}

	public class StormParticle(int style) : ABasicParticle
	{
		public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal(typeof(StormParticle), "StormParticle", false);

		public float Opacity;
		public int TimeToLive = 60;

		private readonly int _style = style;
		private int _timeSinceSpawn;

		public override void Update(ref ParticleRendererSettings settings)
		{
			base.Update(ref settings);

			if (++_timeSinceSpawn > TimeToLive)
			{
				ShouldBeRemovedFromRenderer = true;
			}
			else
			{
				Velocity *= 0.95f;
				Rotation += Velocity.X * 0.01f;

				int halfTime = TimeToLive / 2;

				if (_timeSinceSpawn > halfTime)
					Opacity -= 1f / halfTime;
			}
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
		{
			Texture2D texture = Texture.Value;
			Vector2 position = settings.AnchorPosition + LocalPosition;
			float frame = (float)_timeSinceSpawn / TimeToLive;
			Rectangle source = texture.Frame(2, 5, _style, (int)(EaseFunction.EaseCubicIn.Ease(frame) * 5f), -2, -2);

			spritebatch.Draw(texture, position, source, Color.White * Opacity, Rotation, source.Size() / 2, Scale, default, 0);
		}
	}

	public sealed class StormGlyphPlayer : ModPlayer
	{
		internal int _cooldown;
		public bool Active => Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<StormGlyph>();

		public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			if (Active)
				velocity *= 1.4f;		
		}

		public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (Active)
			{				
				if (_cooldown <= 0)
				{
					for (float k = 0; k < 6.28f; k += 0.1f)
					{
						float x = (float)Math.Cos(k) * 30;
						float y = (float)Math.Sin(k) * 10;

						ParticleHandler.SpawnParticle(new SmokeCloud(position + velocity,
							velocity * 0.15f + new Vector2(x, y).RotatedBy(velocity.ToRotation() + MathHelper.PiOver2) * 0.05f, Color.WhiteSmoke * 0.05f, Main.rand.NextFloat(0.02f, 0.07f), EaseFunction.EaseQuadOut, 60, false));

						ParticleHandler.SpawnParticle(new SmokeCloud(position + velocity * 2f,
							velocity * 0.2f + new Vector2(x, y).RotatedBy(velocity.ToRotation() + MathHelper.PiOver2) * 0.06f, Color.LightCyan * 0.05f, Main.rand.NextFloat(0.02f, 0.07f), EaseFunction.EaseQuadOut, 60, false));
					}

					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SwordSlash1") with { Volume = 1.5f, PitchVariance = 0.2f }, position);
				}
				else
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SmallProjectileWoosh_1") with { Volume = 2f, PitchVariance = 0.2f }, position);
			}

			return base.Shoot(item, source, position, velocity, type, damage, knockback);
		}

		public override void ResetEffects()
		{
			if (_cooldown > 0)
				_cooldown--;
		}
	}

	public sealed class StormGlyphGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.friendly;

		private readonly ParticleRenderer _stormParticleRenderer = new();
		private VertexTrail[] _trails;

		public bool doVisuals;
		public bool doWindBurst;
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (source is IEntitySource_WithStatsFromItem { Item: Item item } && item.GetGlyph() is GlyphItem.GlyphType itemGlyph && itemGlyph.ItemType == ModContent.ItemType<StormGlyph>())
			{
				Player player = Main.player[projectile.owner];

				var mp = player.GetModPlayer<StormGlyphPlayer>();

				if (mp._cooldown <= 0)
				{
					doWindBurst = true;
					mp._cooldown = 120;
				}

				doVisuals = true;
				projectile.netUpdate = true;
			}
		}

		public override void AI(Projectile projectile)
		{
			if (doVisuals)
			{
				if (!Main.dedServ)
				{
					if (_trails == null)
						CreateTrail(projectile);

					foreach (VertexTrail trail in _trails)
						trail.Update();
				}

				if (projectile.timeLeft % 3 == 0)
				{
					Vector2 pos = projectile.Center;

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, projectile.velocity * Main.rand.NextFloat(0.3f), Color.White * 0.15f, 0.03f, EaseFunction.EaseQuadOut, 60, false));

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, projectile.velocity * Main.rand.NextFloat(0.3f), Color.LightCyan * 0.25f, 0.02f, EaseFunction.EaseQuadOut, 60, false));
				}

				if (doWindBurst)
				{
					if (Main.rand.NextBool(17))
					{
						Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(20f, 20f);
						Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
						float scale = Main.rand.NextFloat(0.1f, 0.3f);

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 90, 12, Main.rand.NextBool() ? SpinAction : SpinAction_2));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 90, 12, Main.rand.NextBool() ? SpinAction : SpinAction_2));
					}

					StormMetaballSystem.Add(new StormParticle(Main.rand.Next(2))
					{
						LocalPosition = projectile.Center,
						Scale = Vector2.One * Main.rand.NextFloat(0.4f, 0.8f),
						Velocity = projectile.velocity * Main.rand.NextFloat(),
						TimeToLive = 30,
						Opacity = 1f,
						Rotation = Main.rand.NextFloat()
					});
				}

				// yes this is silly
				// yes I think this is the most reasonable way to do this
				static void SpinAction(Particle p)
				{
					p.Velocity *= 0.97f;
					p.Velocity = p.Velocity.RotatedBy(0.08f);
				}

				static void SpinAction_2(Particle p)
				{
					p.Velocity *= 0.97f;
					p.Velocity = p.Velocity.RotatedBy(-0.08f);
				}

				static void DecelerateAction(Particle p)
				{
					p.Velocity *= 0.97f;
				}
			}
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (doVisuals)
			{
				for (int i = 0; i < 4; i++)
				{
					Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
					Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
					float scale = Main.rand.NextFloat(0.1f, 0.3f);

					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 60, 15, Main.rand.NextBool() ? SpinAction : SpinAction_2));
					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 60, 15, Main.rand.NextBool() ? SpinAction : SpinAction_2));

					static void SpinAction(Particle p)
					{
						p.Velocity *= 0.965f;
						p.Velocity = p.Velocity.RotatedBy(0.09f);
					}

					static void SpinAction_2(Particle p)
					{
						p.Velocity *= 0.965f;
						p.Velocity = p.Velocity.RotatedBy(-0.09f);
					}
				}			
			}

			if (doWindBurst)
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SwordSlash1") with { Volume = 1.5f, PitchVariance = 0.2f }, projectile.Center);
				SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 1f, PitchVariance = 0.1f }, projectile.Center);
				SoundEngine.PlaySound(SoundID.DD2_SonicBoomBladeSlash with { Volume = 1f, PitchVariance = 0.2f }, projectile.Center);

				if (Main.myPlayer == projectile.owner)
				{
					ScreenshakeHelper.Shake(target.Center, target.DirectionTo(Main.player[projectile.owner].Center), 1, 4, 10);
					Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<WindBurstProjectile>(), (int)(10 * projectile.knockBack), projectile.knockBack * Main.rand.NextFloat(3f, 5f), projectile.owner).rotation = projectile.velocity.ToRotation();
				}

				for (int i = 0; i < 30; i++)
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(projectile.Center + Main.rand.NextVector2Circular(5f, 5f), projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.7f),
						Color.LightCyan * 0.15f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(30), false));

					ParticleHandler.SpawnParticle(new SmokeCloud(projectile.Center + Main.rand.NextVector2Circular(5f, 5f), projectile.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(0.3f),
						Color.LightCyan * 0.15f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(30), false));
				}

				for (int i = 0; i < 12; i++)
				{
					Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
					Vector2 velocity = projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.7f);
					float scale = Main.rand.NextFloat(0.2f, 0.4f);

					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, Main.rand.Next(25, 45), 7, Main.rand.NextBool() ? SpinAction : SpinAction_2));
					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, Main.rand.Next(25, 45), 7, Main.rand.NextBool() ? SpinAction : SpinAction_2));

					static void SpinAction(Particle p)
					{
						p.Velocity *= 0.965f;
						p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(0.03f));
					}

					static void SpinAction_2(Particle p)
					{
						p.Velocity *= 0.965f;
						p.Velocity = p.Velocity.RotatedBy(-Main.rand.NextFloat(0.03f));
					}
				}
			}
		}

		public override bool PreDraw(Projectile projectile, ref Color lightColor)
		{
			var star = AssetLoader.LoadedTextures["Star"].Value;

			if (doVisuals)
			{
				_stormParticleRenderer.Draw(Main.spriteBatch);

				if (_trails != null)
				{
					foreach (VertexTrail trail in _trails)
					{
						trail.Opacity = 1f;
						trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, Main.spriteBatch.GraphicsDevice);
					}
				}

				if (doWindBurst) 
					Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.White.Additive(), 0f, star.Size() / 2f, 0.15f, 0f, 0f);

			}

			return base.PreDraw(projectile, ref lightColor);
		}

		private void CreateTrail(Projectile proj)
		{
			ITrailCap tCap = new RoundCap();
			ITrailPosition tPos = new EntityTrailPosition(proj);
			ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

			_trails =
			[
				new VertexTrail(new GradientTrail(Color.LightCyan, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 40, 150, -2),
				new VertexTrail(new StandardColorTrail(Color.Gray * 0.25f), tCap, tPos, tShader, 20, 150, -2),
			];

			if (doWindBurst)
				_trails =
				[
					.. _trails,
                    new VertexTrail(new GradientTrail(Color.LightCyan.Additive(), Color.White.Additive() * 0.2f, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 10, 170, -2),
				];
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{

		}

		internal class WindBurstProjectile : ModProjectile
		{
			public override string Texture => AssetLoader.EmptyTexture;

			public override void SetDefaults()
			{
				Projectile.usesLocalNPCImmunity = true;
				Projectile.localNPCHitCooldown = -1;

				Projectile.penetrate = 5;
				Projectile.aiStyle = -1;
				Projectile.timeLeft = 20;

				Projectile.Size = new(12);
				Projectile.friendly = true;
				Projectile.DamageType = DamageClass.Generic;

				Projectile.tileCollide = false;
			}

			public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
			{
				Vector2 rotation = Projectile.rotation.ToRotationVector2();
				Vector2 direction = Projectile.DirectionTo(targetHitbox.Center.ToVector2());

				return Vector2.Dot(rotation, direction) >= Math.Cos(MathHelper.ToRadians(90f) / 2f) && Projectile.Distance(targetHitbox.Center.ToVector2()) < 150f;
			}

			public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
			{
				modifiers.HitDirectionOverride = Main.player[Projectile.owner].Center.X > target.Center.X ? -1 : 1;

				modifiers.HideCombatText();
			}

			public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			{
				target.velocity.Y -= Main.rand.NextFloat(1f, 3.33f);
				target.netUpdate = true;

				int idx = CombatText.NewText(target.getRect(), Color.White, damageDone, hit.Crit);

				ColoredCombatText.AddCombatText(idx, Color.GhostWhite, Color.LightGray);

				for (int i = 0; i < 5; i++)
				{
					Vector2 pos = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);
					Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);
					float scale = Main.rand.NextFloat(0.1f, 0.3f);

					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 60, 3, DecelerateAction));
					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 60, 3, DecelerateAction));
					
					pos = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);
					velocity = Projectile.rotation.ToRotationVector2().RotatedByRandom(0.4f) * Main.rand.NextFloat(5f, 10f);
					scale = Main.rand.NextFloat(0.2f, 0.4f);

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity,
						Color.LightCyan * 0.15f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(30), false));

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity,
						Color.LightCyan * 0.15f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(30), false));
				}

				static void DecelerateAction(Particle p)
				{
					p.Velocity *= 0.97f;
				}
			}
		}
	}

	// can only apply to items that shoot projectiles
	// also no summon weapons
	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && item.shoot > 0 && item.DamageType != DamageClass.Summon;

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(142, 186, 231));
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

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.LightCyan, Color.Cyan, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightGray, Color.White, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.WhiteSmoke.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.WhiteSmoke.Additive() * 0.35f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.LightCyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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
	public static Vector2 FindGroundFromPosition(Vector2 input)
	{
		const int dimensions = 8;

		while (!CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y += dimensions;

		while (CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	public Vector2? tilePosition;

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (tilePosition is null && item.velocity == Vector2.Zero)
			tilePosition = FindGroundFromPosition(item.Center);

		gravity *= 0.33f;

		if (tilePosition.HasValue && Math.Abs(item.Center.Y - tilePosition.Value.Y) < 24 && Math.Abs(item.Center.Y - tilePosition.Value.Y) > 0)
			item.velocity.Y -= 0.12f;

		if (item.velocity.X != 0)
			tilePosition = null;

		if (item.velocity.Y < 0)
		{
			if (Main.rand.NextBool(3))
			{
				Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 2, item.width / 2), -Main.rand.Next(item.height / 4));

				StormMetaballSystem.Add(new StormParticle(Main.rand.Next(2))
				{
					LocalPosition = pos,
					Scale = Vector2.One * Main.rand.NextFloat(0.3f, 0.5f),
					Velocity = Vector2.UnitY * Main.rand.NextFloat(2f),
					TimeToLive = 20,
					Opacity = 1f,
					Rotation = Main.rand.NextFloat()
				});
			}

			if (Main.rand.NextBool(5))
			{
				Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

				ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.UnitY * Main.rand.NextFloat(2f), Color.White * 0.25f, 0.06f, EaseFunction.EaseQuadOut, 60, false));

				ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.UnitY * Main.rand.NextFloat(2f), Color.LightCyan * 0.35f, 0.04f, EaseFunction.EaseQuadOut, 60, false));
			}
		}		

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			Vector2 velocity = Main.rand.NextVector2CircularEdge(2f, 2f);

			float scale = Main.rand.NextFloat(0.1f, 0.3f);

			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 120, 3, SpinAction));
			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 120, 3, SpinAction));
		}

		if (Main.rand.NextBool(10) && item.velocity.Y < 0)
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 2, item.width / 2), -Main.rand.Next(item.height / 4));

			Vector2 velocity = Vector2.UnitY * Main.rand.NextFloat(1f, 3f);

			float scale = Main.rand.NextFloat(0.1f, 0.3f);

			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 60, 3, DecelerateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 60, 3, DecelerateAction));
			
			velocity = Vector2.UnitY * Main.rand.NextFloat(0.5f, 1f);

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity, Color.White * 0.15f, 0.05f, EaseFunction.EaseQuadOut, 60, false));

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity, Color.LightCyan * 0.25f, 0.03f, EaseFunction.EaseQuadOut, 60, false));
		}

		static void SpinAction(Particle p)
		{
			p.Velocity *= 0.97f;
			p.Velocity = p.Velocity.RotatedBy(0.05f);
		}

		static void DecelerateAction(Particle p)
		{
			p.Velocity *= 0.95f;
		}
	}
}