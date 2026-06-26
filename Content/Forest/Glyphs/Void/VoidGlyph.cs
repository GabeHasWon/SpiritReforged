using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using System.Linq;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Forest.Glyphs.Void;

public class VoidGlyph : GlyphItem
{
	public sealed class VoidParticle : Particle
	{
		public VoidParticle(Vector2 position, Vector2 velocity, Color color, float rotation, float scale, int maxTime)
		{
			Position = position;
			Color = color;
			Rotation = rotation;
			Scale = scale;
			MaxTime = maxTime;
			Velocity = velocity;

			SingularityVisualSystem.particles.Add(this);
		}

		public override void Update()
		{
			Velocity *= 0.97f;
			Rotation += Velocity.Length() * 0.02f;
		}

		public override void OnKill() => SingularityVisualSystem.particles.Remove(this);

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;

			spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, Color * 0.33f, 0, bloomtexture.Size() / 2, Scale * (1f - TimeActive / (float)MaxTime), SpriteEffects.None, 0);
		}

		public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

		public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	}

	// Visual system that uses a Render Target to render all singularities for the void glyph
	public sealed class SingularityVisualSystem : ModSystem
	{
		private static readonly ModTarget2D SingularityTarget = new(static () => projectiles.Count != 0 || particles.Count != 0, DrawTarget);

		public static List<CollapseProjectile> projectiles = [];
		public static List<VoidParticle> particles = [];

		// drawing a bloom map here for the input to our shader
		private static void DrawTarget(SpriteBatch spriteBatch)
		{
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (CollapseProjectile singularity in projectiles)
			{
				// this should theoretically not happen, but just safety check
				if (singularity is null || !singularity.Projectile.active)
					continue;

				if (!singularity._dying)
					continue;

				var projectile = singularity.Projectile;

				float progress = EaseBuilder.EaseQuinticInOut.Ease(1f - projectile.timeLeft / 60f);
				float intensity = singularity._stacksOnDeath / 10f;

				// Shader uses the G channel for the progress of the black hole.
				// Shader uses the B channel for the stacks of the black hole (increases singularity intensity)
				Color dataColor = new Color(1f, progress, intensity, 1f);
				
				float sizeInterpolant = progress < 0.5f ? progress / 0.5f : 1f - (progress - 0.5f) / 0.5f;
				float visualScale = (1.2f + intensity * 0.2f) * sizeInterpolant;

				spriteBatch.Draw(bloom, projectile.Center - Main.screenPosition, null, dataColor, 0f, bloom.Size() / 2f, visualScale, 0f, 0f);
			}

			foreach (VoidParticle particle in particles)
			{
				if (particle is null)
					continue;

				float progress = particle.TimeActive / (float)particle.MaxTime;

				Color dataColor = new Color(1f, progress, 0.5f, 1f);
				float visualScale = particle.Scale * (1f - progress);

				spriteBatch.Draw(bloom, particle.Position - Main.screenPosition, null, dataColor, 0f, bloom.Size() / 2f, visualScale, 0f, 0f);
			}
		}

		public override void PostUpdateEverything()
		{
			if (!Main.dedServ)
			{
				if (SingularityTarget is not null && SingularityTarget.Active)
				{
					if (!Main.dedServ && !Filters.Scene["SpiritReforged:VoidGlyphSingularity"].IsActive())
						Filters.Scene.Activate("SpiritReforged:VoidGlyphSingularity");

					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(SingularityTarget);
					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseIntensity(2f * Main.GameViewMatrix.Zoom.X);
				}
				else if (Filters.Scene["SpiritReforged:VoidGlyphSingularity"].IsActive())
				{
					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(TextureAssets.Npc[0]);
					Filters.Scene.Deactivate("SpiritReforged:VoidGlyphSingularity");
				}
			}
		}
	}

	public sealed class VoidPlayer : ModPlayer
	{
		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<VoidGlyph>())
				ProcSingularity(target, damageDone);
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<VoidGlyph>())
				ProcSingularity(target, damageDone);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (target.TryGetGlobalNPC(out VoidNPC voidNPC) && voidNPC.stacks > 0)
				ProcSingularity(target, damageDone); //Allow any damage source to contribute to the singularity if it already exists
		}

		public void ProcSingularity(NPC target, int damageDone)
		{
			if (!Main.rand.NextBool(5))
				return;

			VoidNPC.AddStack(Player.whoAmI, target.whoAmI, damageDone / 2);

			for (int i = 0; i < 1 + Main.rand.Next(0, 3); i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(6f, 3f);
				float rotation = Main.rand.NextFloat(6.28f);

				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, velocity, Color.Purple.Additive(), 0.2f, 35, 0, DecelerateAction)
				{
					Rotation = rotation
				});

				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, velocity, Color.LightPink.Additive(), 0.1f, 35, 0, DecelerateAction, false)
				{
					Rotation = rotation
				});

				static void DecelerateAction(Particle p)
				{
					p.Velocity *= 0.95f;
					p.Rotation += p.Velocity.Length() * 0.1f;
				}

				velocity = Main.rand.NextVector2Circular(4f, 4f);
				float scale = Main.rand.NextFloat(0.1f, 0.3f);

				bool rotDir = Main.rand.NextBool();

				ParticleHandler.SpawnParticle(new GlowParticle(target.Center, velocity, Color.Purple.Additive(), scale, 90, 12, rotDir ? SpinAction : SpinAction_2));
				ParticleHandler.SpawnParticle(new GlowParticle(target.Center, velocity, Color.White.Additive(), scale * 0.5f, 90, 12, rotDir ? SpinAction : SpinAction_2));

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
			}
		}
	}

	public sealed class VoidNPC : GlobalNPC
	{
		public const int COOLDOWN_TIME = 120;

		public override bool InstancePerEntity => true;

		public const int MAX_STACKS = 15;
		public const int COLLAPSE_TIME = 900;
		
		public int stacks;
		public int cooldown;
		public int collapseDamage;

		public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.CanBeChasedBy();

		public static void AddStack(int playerIndex, int targetIndex, int damageDealt, int stacksToAdd = 1)
		{
			Player player = Main.player[playerIndex];
			NPC target = Main.npc[targetIndex];

			if (!target.TryGetGlobalNPC(out VoidNPC voidNPC) || voidNPC.cooldown > 0 || voidNPC.stacks >= MAX_STACKS)
				return;

			if (voidNPC.stacks <= 0)
			{
				var p = Projectile.NewProjectileDirect(player.GetSource_OnHit(target, "SpiritReforged: Void Glyph Apply"), target.Center, Vector2.Zero, ModContent.ProjectileType<CollapseProjectile>(), 0, 0, playerIndex, targetIndex);
				p.timeLeft = COLLAPSE_TIME;
			}
			else if (voidNPC.stacks + stacksToAdd < MAX_STACKS)
			{
				Projectile p = Main.projectile.Where(p => p.ModProjectile is CollapseProjectile && (p.ModProjectile as CollapseProjectile).TargetIndex == targetIndex).FirstOrDefault();

				if (p != default)
					(p.ModProjectile as CollapseProjectile).rotationTimer = 60;
			}

			voidNPC.stacks += stacksToAdd;

			if (voidNPC.stacks > MAX_STACKS)
				voidNPC.stacks = MAX_STACKS;

			voidNPC.collapseDamage += damageDealt;

			SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 2f, Pitch = 0.1f * voidNPC.stacks }, target.Center);
			SoundEngine.PlaySound(Wisp.Hit with { Volume = 2f, Pitch = -0.1f * voidNPC.stacks }, target.Center);
		}

		public override void ResetEffects(NPC npc)
		{
			if (cooldown > 0)
				cooldown--;
		}
	}

	public sealed class CollapseProjectile : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public bool _dying;
		public int _stacksOnDeath;

		public int rotationTimer;
		public float starRotation;

		public Vector2 pos;

		public int TargetIndex => (int)Projectile.ai[0];
		public NPC Target => Main.npc[TargetIndex];

		public override void Load()
		{
			if (!Main.dedServ)
			{
				var shader = ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/VoidGlyphSingularity");
				Filters.Scene["SpiritReforged:VoidGlyphSingularity"] = new Filter(new ScreenShaderData(shader, "ScreenPass"), EffectPriority.VeryHigh);
			}
		}

		public override void SetDefaults()
		{
			Projectile.Size = new(32);
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;

			Projectile.DamageType = DamageClass.Generic;
			Projectile.friendly = true;

			Projectile.penetrate = 1;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool? CanDamage() => _dying && Projectile.timeLeft < 30;

		public override bool? CanHitNPC(NPC target) => target.active ? target.whoAmI == TargetIndex : null;

		public override void AI()
		{
			if (rotationTimer > 0)
			{
				starRotation += rotationTimer * 0.001f;
				rotationTimer--;
			}
			else
				starRotation *= 0.75f;

			if (Target is null || !Target.active)
			{
				if (!_dying)
				{
					Projectile.Kill();
					return;
				}			
			}

			var gnpc = Target.GetGlobalNPC<VoidNPC>();

			if (_dying)
			{
				if (Projectile.timeLeft == 30)
				{
					for (int i = 0; i < 4; i++)
					{
						Vector2 velocity = Main.rand.NextVector2Circular(12f, 2f);

						float rotation = Main.rand.NextFloat(6.28f);

						ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center, velocity, Color.Purple.Additive(), 0.2f, 35, 0, DecelerateAction)
						{
							Rotation = rotation
						});

						ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center, velocity, Color.LightPink.Additive(), 0.1f, 35, 0, DecelerateAction)
						{
							Rotation = rotation
						});

						velocity = Main.rand.NextVector2Circular(8f, 0.5f).RotatedByRandom(0.3f);

						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.Purple.Additive(), 0.5f, 40, 3, DecelerateAction));
						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.LightPink.Additive(), 0.3f, 40, 3, DecelerateAction));

						ParticleHandler.SpawnParticle(new VoidParticle(Projectile.Center, Main.rand.NextVector2CircularEdge(7f, 2f) * Main.rand.NextFloat(0.75f, 1f), Color.Purple.Additive(), 0f, Main.rand.NextFloat(0.2f, 0.25f), 45));

						static void DecelerateAction(Particle p)
						{
							p.Velocity *= 0.95f;

							p.Rotation += p.Velocity.Length() * 0.1f;
						}
					}
				}

				if (Projectile.timeLeft > 30)
				{
					float progressTillHit = (Projectile.timeLeft - 30f) / 30f;
					
					if (Main.rand.NextBool(5))
					{
						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(30f, 30f) * progressTillHit;

						Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
						float scale = Main.rand.NextFloat(0.1f, 0.3f);

						bool rotDir = Main.rand.NextBool();

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Purple.Additive(), scale, 90, 12, rotDir ? SpinAction : SpinAction_2));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 90, 12, rotDir ? SpinAction : SpinAction_2));
					}

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
				}
			}

			int stacks = gnpc.stacks;

			if (_dying)
				stacks = _stacksOnDeath;

			Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * (stacks / 10f));

			if (pos != Vector2.Zero)
				Projectile.Center = pos;

			if (Projectile.position != Projectile.oldPosition)
				Projectile.netUpdate = true;

			if ((Projectile.timeLeft == 1 || gnpc.stacks >= VoidNPC.MAX_STACKS) && !_dying)
			{
				SingularityVisualSystem.projectiles.Add(this);
				SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 3f, Pitch = -0.5f }, Projectile.Center);

				_dying = true;
				Projectile.timeLeft = 60;

				_stacksOnDeath = gnpc.stacks;

				Projectile.damage = gnpc.collapseDamage;
				Projectile.ArmorPenetration = gnpc.stacks;

				gnpc.cooldown = VoidNPC.COOLDOWN_TIME;
				gnpc.stacks = 0;
				gnpc.collapseDamage = 0;
			}

			if (Target is not null)
			{
				pos = Target.Center;
			}
		}

		public override void OnKill(int timeLeft) => SingularityVisualSystem.projectiles.Remove(this);

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(Wisp.Death with { Volume = 2f, Pitch = -0.5f}, target.Center);
			//pos = target.Center;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(79);
			
			int stacks = -1;

			if (!_dying)
			{
				if (Target is not null)
				{
					var gnpc = Target.GetGlobalNPC<VoidNPC>();
					stacks = gnpc.stacks;
				}	
			}
			else
			{
				stacks = _stacksOnDeath;
			}

			if (stacks < 0)
				return false;

			var star = AssetLoader.LoadedTextures["Star"].Value;
			var starNonPreMult = TextureAssets.Projectile[79].Value;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			var bloomNonPreMult = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			float sin = (float)Math.Sin(Projectile.timeLeft);
			float cos = Math.Abs((float)Math.Sin(Projectile.timeLeft * 0.02f));

			float x = 0.15f * stacks;
			float y = 0.05f * stacks;

			Vector2 scale = new Vector2(x + 0.02f * sin, y + 0.02f * sin);

			Vector2 offset = Vector2.Zero;
			if (Projectile.timeLeft < 60 && !_dying)
				offset = Main.rand.NextVector2Circular(2f, 2f) * (1f - Projectile.timeLeft / 60f);

			if (_dying)
			{
				float progress = EaseBuilder.EaseQuarticInOut.Ease(1f - Projectile.timeLeft / 60f);

				if (progress < 0.5f)
				{
					float lerp = progress / 0.5f;

					Color c = new Color(60, 0, 65, 0);

					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0), 0f, bloom.Size() / 2f, scale.X * 0.4f * lerp, 0f, 0f);
					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0), 0f, bloom.Size() / 2f, scale.X * 0.3f * lerp, 0f, 0f);
					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, c, 0f, bloom.Size() / 2f, scale.X * 0.3f * lerp, 0f, 0f);
					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive(), 0f, bloom.Size() / 2f, scale.X * 0.25f * lerp, 0f, 0f);
				}
				else
				{
					float lerp = 1f - (progress - 0.5f) / 0.5f;

					Color c = new Color(60, 0, 65, 0);

					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0), 0f, bloom.Size() / 2f, scale.X * 0.4f * lerp, 0f, 0f);
					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0), 0f, bloom.Size() / 2f, scale.X * 0.3f * lerp, 0f, 0f);
					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, c, 0f, bloom.Size() / 2f, scale.X * 0.3f * lerp, 0f, 0f);
					Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive(), 0f, bloom.Size() / 2f, scale.X * 0.25f * lerp, 0f, 0f);
				}

				if (Projectile.timeLeft > 30)
				{
					float progressTillHit = (Projectile.timeLeft - 30f) / 30f;
					scale *= EaseBuilder.EaseQuinticOut.Ease(progressTillHit);
				}
				else
					scale *= 0f;
			}

			Color[] voidColors = [new(255, 65, 255, 0), new(255, 65, 185, 0), new(211, 65, 255, 0), new(166, 65, 255, 0)];

			if (starRotation > 0)
			{
				float progress = EaseBuilder.EaseQuarticInOut.Ease(rotationTimer / 60f);

				float _scale = 0.03f * stacks;

				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * progress, starRotation, star.Size() / 2f, _scale * progress, 0f, 0f);
				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * progress * 0.75f, starRotation, star.Size() / 2f, _scale * 0.66f * progress, 0f, 0f);
			}

			if (scale.LengthSquared() > 0f)
			{
				float progressTillHit = EaseBuilder.EaseQuinticOut.Ease(1f - (Projectile.timeLeft - 30f) / 30f);

				Color c = DrawHelpers.MulticolorLerp(cos, voidColors);

				if (_dying)
					c = Color.Lerp(c, new Color(60, 0, 65, 0), progressTillHit);

				Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, c, 0f, starNonPreMult.Size() / 2f, scale, 0f, 0f);
				Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.White.Additive() * 0.75f, 0f, starNonPreMult.Size() / 2f, scale * 0.65f, 0f, 0f);

				if (_dying)
				{
					Main.spriteBatch.End();
					Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

					Main.spriteBatch.Draw(bloomNonPreMult, Projectile.Center - Main.screenPosition, null, Color.Black, 0f, bloomNonPreMult.Size() / 2f, x * 0.2f * progressTillHit, 0f, 0f);
					Main.spriteBatch.Draw(bloomNonPreMult, Projectile.Center - Main.screenPosition, null, Color.Black * 0.5f, 0f, bloomNonPreMult.Size() / 2f, x * 0.4f * progressTillHit, 0f, 0f);
					
					Main.spriteBatch.Draw(starNonPreMult, Projectile.Center - Main.screenPosition, null, new Color(60, 0, 65) * 0.6f, 0f, starNonPreMult.Size() / 2f, scale * 0.9f * progressTillHit, 0f, 0f);
					Main.spriteBatch.Draw(starNonPreMult, Projectile.Center - Main.screenPosition, null, Color.Black * 0.4f, 0f, starNonPreMult.Size() / 2f, scale * 0.7f * progressTillHit, 0f, 0f);

					Main.spriteBatch.End();
					Main.spriteBatch.BeginDefault();
				}
			}

			if (_dying)
			{
				float progress = EaseBuilder.EaseQuarticInOut.Ease(1f - Projectile.timeLeft / 60f);

				x = 0.2f * stacks;
				y = 0.1f * stacks;

				scale = new Vector2(x + 0.02f * sin, y + 0.02f * sin);

				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * (1f - progress) * 1.5f, 0f, star.Size() / 2f, scale * progress, 0f, 0f);
				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * (1f - progress) * 0.75f, 0f, star.Size() / 2f, scale * 0.65f * progress, 0f, 0f);

				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(60, 0, 65, 0) * (1f - progress) * 1.5f, 0f, star.Size() / 2f, scale * 0.5f * progress, 0f, 0f);
				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * (1f - progress) * 0.75f, 0f, star.Size() / 2f, scale * 0.25f * progress, 0f, 0f);
			}

			return false;
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(225, 63, 255));
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["noiseCrystal"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

		Color main = Color.Lerp(new(225, 63, 255), new(166, 63, 255), sin);
		if (sin > 0.5f)
			main = Color.Lerp(main, Color.Black, sin);

		effect.Parameters["uColor1"].SetValue(main.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(new(255, 63, 230), new(255, 63, 192), cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Black * 0.5f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.Violet * 0.25f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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

		if (sin > 0)
			spriteBatch.Draw(whiteTexture, parameters.Position, parameters.Source, Color.Black * 0.5f * sin, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));

		if (Main.rand.NextBool(90) && sin < 0.33f)
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Purple.Additive(), 0.2f, 35, 0)
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
		else if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new VoidParticle(pos, Vector2.Zero, Color.Purple.Additive(), 0f, 0.25f, 40));

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos + new Vector2(0, 2), Vector2.Zero, Color.Purple.Additive(), 0.2f, 35, 0)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos + new Vector2(0, 2), Vector2.Zero, Color.LightPink.Additive(), 0.15f, 30, 0, AddLight: false)
			{
				Rotation = 0f,
				Layer = ParticleLayer.AboveItem
			});
		}
	}
}