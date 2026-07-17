using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.CombatTextCommon;
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
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Void;

public class VoidGlyph : GlyphItem
{
	public sealed class VoidParticle : Particle
	{
		internal Entity _ent = null;
		internal Vector2 _offset;

		bool initialized = false;

		public VoidParticle(Vector2 position, Vector2 velocity, Color color, float rotation, float scale, int maxTime, Entity attached = null)
		{
			Position = position;
			Color = color;
			Rotation = rotation;
			Scale = scale;
			MaxTime = maxTime;
			Velocity = velocity;

			_ent = attached;

			if (_ent != null)
				_offset = Position - _ent.Center;
		}

		public override void Update()
		{
			if (!initialized)
			{
				SingularityVisualSystem.particles.Add(this);
				initialized = true;
			}

			if (_ent != null)
			{
				if (!_ent.active)
				{
					_ent = null;
					return;
				}

				Position = _ent.Center + _offset;
				_offset += Velocity;
			}

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
			spriteBatch.Begin(SpriteSortMode.Deferred, DrawHelpers.AdditiveNoAlpha, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (CollapseProjectile singularity in projectiles)
			{
				// this should theoretically not happen, but just safety check
				if (singularity is null || !singularity.Projectile.active)
					continue;

				if (!singularity._dying)
					continue;

				var projectile = singularity.Projectile;

				float progress = singularity.Progress;
				float intensity = singularity.Intensity;

				// Shader uses the G channel for the progress of the black hole.
				// Shader uses the B channel for the stacks of the black hole (increases singularity intensity)
				var dataColor = new Color(1f, progress, intensity, 1f);

				float sizeInterpolant = progress < 0.5f ? progress / 0.5f : 1f - (progress - 0.5f) / 0.5f;
				float visualScale = (0.8f + singularity._stacksOnDeath / (float)VoidNPC.MAX_STACKS * 0.5f) * sizeInterpolant;

				Vector2 actualScale = new Vector2(visualScale);

				if (singularity._pulseTimer > 0)
				{
					actualScale = new Vector2(visualScale * MathHelper.Lerp(1.0f, 1.1f, singularity._pulseTimer / 60f), visualScale * MathHelper.Lerp(1.0f, 0.9f, singularity._pulseTimer / 60f));
				}

				spriteBatch.Draw(bloom, projectile.Center - Main.screenPosition, null, dataColor, 0f, bloom.Size() / 2f, actualScale, 0f, 0f);
			}

			foreach (VoidParticle particle in particles)
			{
				if (particle is null)
					continue;

				float progress = particle.TimeActive / (float)particle.MaxTime;

				var dataColor = new Color(1f, progress, 0.5f, 1f);
				float visualScale = particle.Scale * (1f - progress);

				spriteBatch.Draw(bloom, particle.Position - Main.screenPosition, null, dataColor, 0f, bloom.Size() / 2f, visualScale, 0f, 0f);
			}
		}

		public override void PostUpdateEverything()
		{
			if (!Main.dedServ)
				if (SingularityTarget is not null && SingularityTarget.Active)
				{
					if (!Main.dedServ && !Filters.Scene["SpiritReforged:VoidGlyphSingularity"].IsActive())
						Filters.Scene.Activate("SpiritReforged:VoidGlyphSingularity");

					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(SingularityTarget);
					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(AssetLoader.LoadedTextures["swirlNoise"], 1);
					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseIntensity(2f * Main.GameViewMatrix.Zoom.X);
				}
				else if (Filters.Scene["SpiritReforged:VoidGlyphSingularity"].IsActive())
				{
					Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(TextureAssets.Npc[0]);
					Filters.Scene.Deactivate("SpiritReforged:VoidGlyphSingularity");
				}
		}
	}

	public sealed class VoidPlayer : ModPlayer
	{
		public bool canApplySingularity = true;
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

		// One hit can proc singularity twice with this
		// It's not terrible though, allows players to get some stacks easier
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (target.TryGetGlobalNPC(out VoidNPC voidNPC) && voidNPC.stacks > 0)
				ProcSingularity(target, damageDone); //Allow any damage source to contribute to the singularity if it already exists
		}

		public void ProcSingularity(NPC target, int damageDone)
		{
			if (!Main.rand.NextBool(2))
				return;

			VoidNPC.AddStack(Player.whoAmI, target.whoAmI, damageDone / 2);
		}
	}

	// TODO: Make defense reduction a ModBuff (?)
	public sealed class VoidNPC : GlobalNPC
	{
		public const int COOLDOWN_TIME = 180;
		public const float DEFENSE_REDUCTION_MULT = 0.8f; // % amount of defense reduction for the defense reduction debuff

		public override bool InstancePerEntity => true;

		public const int MAX_STACKS = 15;
		public const int COLLAPSE_TIME = 900;

		public int stacks;
		public int cooldown;
		public int collapseDamage;

		public int defenseReductionTimer; 

		public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.CanBeChasedBy();

		public static void AddStack(int playerIndex, int targetIndex, int damageDealt, int stacksToAdd = 1)
		{
			Player player = Main.player[playerIndex];
			NPC target = Main.npc[targetIndex];

			if (!target.TryGetGlobalNPC(out VoidNPC voidNPC) || voidNPC.cooldown > 0 || voidNPC.stacks >= MAX_STACKS)
				return;

			Projectile p = Main.projectile.Where(p => p.ModProjectile is CollapseProjectile && (p.ModProjectile as CollapseProjectile).TargetIndex == targetIndex && !(p.ModProjectile as CollapseProjectile)._dying).FirstOrDefault();

			if (voidNPC.stacks <= 0 && player.ownedProjectileCounts[ModContent.ProjectileType<CollapseProjectile>()] <= 0 && player.GetModPlayer<VoidPlayer>().canApplySingularity)
			{
				var singularity = Projectile.NewProjectileDirect(player.GetSource_OnHit(target, "SpiritReforged: Void Glyph Apply"), target.Center, Vector2.Zero, ModContent.ProjectileType<CollapseProjectile>(), 0, 7f, playerIndex, targetIndex);
				singularity.timeLeft = COLLAPSE_TIME;

				p = singularity;
				player.GetModPlayer<VoidPlayer>().canApplySingularity = false;
			}

			if (p != default)
			{
				if (voidNPC.stacks + stacksToAdd < MAX_STACKS)
				{
					if (p != default)
						(p.ModProjectile as CollapseProjectile).rotationTimer = 60;
				}

				voidNPC.stacks += stacksToAdd;

				if (voidNPC.stacks > MAX_STACKS)
					voidNPC.stacks = MAX_STACKS;

				voidNPC.collapseDamage += damageDealt / 4;
				voidNPC.collapseDamage += Main.hardMode ? 10 : 3;

				SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 2f, Pitch = 0.1f * voidNPC.stacks }, target.Center);
				SoundEngine.PlaySound(Wisp.Hit with { Volume = 2f, Pitch = -0.1f * voidNPC.stacks }, target.Center);

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

		public override void ResetEffects(NPC npc)
		{
			if (cooldown > 0)
				cooldown--;

			if (defenseReductionTimer > 0)
				defenseReductionTimer--;
		}

		public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (defenseReductionTimer > 0)
				modifiers.Defense *= DEFENSE_REDUCTION_MULT;
		}

		public override void AI(NPC npc)
		{
			if (defenseReductionTimer > 0 && Main.rand.NextBool(240))
			{
				if (!Main.dedServ)
				{
					ParticleHandler.SpawnParticle(new VoidParticle(npc.Center, Vector2.Zero, Color.Purple.Additive(), 0f, 0.3f, 60, npc));
				}
			}
		}

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (defenseReductionTimer > 0)
			{
				Color darken = Color.Lerp(drawColor, Color.Black, 0.5f);

				if (defenseReductionTimer < 60)
					drawColor = Color.Lerp(drawColor, darken, defenseReductionTimer / 60f);
				else
					drawColor = darken;
			}
		}
	}

	public static readonly SoundStyle VoidHit1 = new("SpiritReforged/Assets/SFX/Glyph/VoidGlyphExplode1")
	{
		Volume = 1.25f
	};

	public static readonly SoundStyle VoidHit2 = new("SpiritReforged/Assets/SFX/Glyph/VoidGlyphExplode2")
	{
		Volume = 1.25f
	};

	public sealed class CollapseProjectile : ModProjectile
	{
		public const int SINGULARITY_LIFETIME = 180;
		public const int PULSE_COUNT = 5;
		public override string Texture => AssetLoader.EmptyTexture;

		public int _pulseTimer;
		public int _attackTimer;

		public bool _dying;
		public int _stacksOnDeath;

		public int rotationTimer;
		public float starRotation;

		public Vector2 pos;

		public int TargetIndex => (int)Projectile.ai[0];
		public float Progress
		{
			get => Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}
		public float Intensity
		{
			get => Projectile.ai[2];
			set => Projectile.ai[2] = value;
		}

		public NPC Target => _dying ? null : Main.npc[TargetIndex];

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
			Projectile.Size = new(150);
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;

			Projectile.DamageType = DamageClass.Generic;
			Projectile.friendly = true;

			Projectile.penetrate = -1;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override bool? CanDamage() => _pulseTimer > 20;

		public override void AI()
		{
			if (_pulseTimer > 0)
				_pulseTimer--;

			if (rotationTimer > 0)
			{
				starRotation += rotationTimer * 0.001f;
				rotationTimer--;
			}
			else
				starRotation *= 0.75f;

			if ((Target is null || !Target.active || (Projectile.timeLeft == 1 || _stacksOnDeath >= VoidNPC.MAX_STACKS)) && !_dying)
			{
				SingularityVisualSystem.projectiles.Add(this);
				SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 3f, Pitch = -0.5f }, Projectile.Center);
				
				Projectile.timeLeft = SINGULARITY_LIFETIME;
				Projectile.damage += 5;
				Vector2 oldCenter = Projectile.Center;
				Projectile.Size = new Vector2(100 + _stacksOnDeath * 10);
				Projectile.Center = oldCenter;

				if (Target is not null)
				{
					Target.TryGetGlobalNPC<VoidNPC>(out var gnpc);

					if (gnpc is not null)
					{
						Projectile.velocity = Target.velocity * 0.05f;

						gnpc.cooldown = VoidNPC.COOLDOWN_TIME;
						gnpc.stacks = 0;
						gnpc.collapseDamage = 0;
					}
				}			

				_dying = true;
			}

			if (_dying)
			{
				float progress = 1f - Projectile.timeLeft / (float)SINGULARITY_LIFETIME;

				if (_pulseTimer > 0)
					Projectile.Center += Main.rand.NextVector2Circular(2.5f, 2.5f) * _pulseTimer / 30f;

				if (progress < 0.15f)
				{
					float interpolant = progress / 0.15f;

					Progress = MathHelper.Lerp(0, 0.5f, EaseBuilder.EaseQuinticIn.Ease(interpolant));
					Intensity = interpolant;
				}
				else
				{
					if (progress > 0.9f)
					{
						float interpolant = (progress - 0.9f) / 0.1f;

						Progress = MathHelper.Lerp(0.35f, 0f, EaseBuilder.EaseQuinticOut.Ease(interpolant));
						Intensity = MathHelper.Lerp(0.7f, 0f, EaseBuilder.EaseQuinticOut.Ease(interpolant));
					}
					else
					{
						if (Main.rand.NextBool())
						{
							Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(75f, 75f) * Intensity;
							Vector2 velocity = pos.DirectionTo(Projectile.Center) * 3f;

							Dust.NewDustPerfect(pos, DustID.Granite, velocity, 230, default, Main.rand.NextFloat(1f, 1.5f)).noGravity = true;
						}

						if (Main.rand.NextBool(9))
						{
							Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(90f, 90f) * Intensity;
							Vector2 velocity = pos.DirectionTo(Projectile.Center) * 3f;

							ParticleHandler.SpawnParticle(new VoidParticle(pos, velocity, Color.Purple.Additive(), 0f, Main.rand.NextFloat(0.15f, 0.3f) * Intensity, 45));
						}

						int leftOverTime = SINGULARITY_LIFETIME - SINGULARITY_LIFETIME / 4;

						if (_attackTimer++ % (leftOverTime / PULSE_COUNT) == 0)
						{
							Intensity -= 0.3f / PULSE_COUNT;
							Progress -= 0.15f / PULSE_COUNT;
							_pulseTimer = 30;

							if (!Main.dedServ)
							{
								for (int i = 0; i < 4; i++)
								{
									Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
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

									ParticleHandler.SpawnParticle(new ImpactLine(pos, Main.rand.NextVector2CircularEdge(9f, 9f) * Main.rand.NextFloat(0.9f, 1.1f), Color.Purple * 0.5f, new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 60, 0.9f));

									ParticleHandler.SpawnParticle(new ImpactLine(pos, Main.rand.NextVector2CircularEdge(9f, 9f) * Main.rand.NextFloat(0.9f, 1.1f), Color.Black * 0.5f, new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 60, 0.9f));

									static void DecelerateAction(Particle p)
									{
										p.Velocity *= 0.95f;

										p.Rotation += p.Velocity.Length() * 0.1f;
									}
								}

								SoundEngine.PlaySound(Main.rand.NextBool() ? VoidHit1 : VoidHit2, Projectile.Center);
							}					
						}
					}
				}

				Projectile.velocity *= 0.97f;

				return;
			}

			Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * (_stacksOnDeath / (float)VoidNPC.MAX_STACKS));

			if (pos != Vector2.Zero)
				Projectile.Center = pos;

			if (Projectile.position != Projectile.oldPosition)
				Projectile.netUpdate = true;

			if (Target is not null)
			{
				Target.TryGetGlobalNPC<VoidNPC>(out var gnpc);

				pos = Target.Center;
				_stacksOnDeath = gnpc.stacks;
				Projectile.damage = 5 + gnpc.collapseDamage;
				Projectile.ArmorPenetration = gnpc.stacks;
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (_dying)
			{
				Main.player[Projectile.owner].GetModPlayer<VoidPlayer>().canApplySingularity = true;
				SoundEngine.PlaySound((Main.rand.NextBool() ? VoidHit1 : VoidHit2) with { Volume = 0.5f, Pitch = -0.3f}, Projectile.Center);
				SingularityVisualSystem.projectiles.Remove(this);
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.HideCombatText();
			modifiers.HitDirectionOverride = target.Center.X < Projectile.Center.X ? 1 : -1;
			float distance = target.Distance(Projectile.Center);
			if (distance < 150)
				modifiers.Knockback *= MathHelper.Lerp(0.5f, 1f, distance / 150f);

			modifiers.Knockback *= 2f - target.knockBackResist;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			var rect = target.getRect();

			int damage = Math.Max(damageDone, 1);

			int idx = CombatText.NewText(rect, Color.White, damage, hit.Crit);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendData(MessageID.CombatTextInt, number: (int)Color.White.PackedValue, number2: rect.X, number3: rect.Y, number4: damage);

			ColoredCombatText.AddCombatText(idx, Color.Purple, Color.DarkViolet);

			if (target.TryGetGlobalNPC<VoidNPC>(out var gnpc))
				gnpc.defenseReductionTimer = 300;
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
				stacks = _stacksOnDeath;

			if (stacks < 0)
				return false;

			var star = AssetLoader.LoadedTextures["Star"].Value;
			var starNonPreMult = TextureAssets.Projectile[79].Value;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			var bloomNonPreMult = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			float sin = (float)Math.Sin(Projectile.timeLeft);
			float cos = Math.Abs((float)Math.Sin(Projectile.timeLeft * 0.02f));

			float x = 1f + 0.15f * stacks;
			float y = 0.3f + 0.05f * stacks;

			var scale = new Vector2(x + 0.02f * sin, y + 0.02f * sin);
			Vector2 offset = Vector2.Zero;

			if (Projectile.timeLeft < 60 && _dying)
				scale *= Projectile.timeLeft / 60f;

			if (_dying)
			{
				float progress = Progress * 2f;
				offset = Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * progress;

				var c = new Color(60, 0, 65, 0);

				Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.4f * progress * Intensity, 0f, 0f);
				Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.3f * progress * Intensity, 0f, 0f);
				Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, c * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.3f * progress * Intensity, 0f, 0f);
				Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.25f * progress * Intensity, 0f, 0f);
				
				float prog = 1f - Projectile.timeLeft / (float)SINGULARITY_LIFETIME;

				if (prog < 0.3f)
				{
					float progressTillHit = 1f - prog / 0.3f;
					scale *= EaseFunction.EaseQuinticOut.Ease(progressTillHit);
				}
				else
					scale *= 0f;
			}

			Color[] voidColors = [new(255, 65, 255, 0), new(255, 65, 185, 0), new(211, 65, 255, 0), new(166, 65, 255, 0)];

			if (starRotation > 0)
			{
				float progress = EaseFunction.EaseQuarticInOut.Ease(rotationTimer / 60f);

				float _scale = 0.06f + 0.04f * stacks;

				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * progress, starRotation, star.Size() / 2f, _scale * progress, 0f, 0f);
				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * progress * 0.75f, starRotation, star.Size() / 2f, _scale * 0.66f * progress, 0f, 0f);
			}

			if (scale.LengthSquared() > 0f)
			{
				float progressTillHit = Progress * 2f;
				if (Projectile.timeLeft < SINGULARITY_LIFETIME - SINGULARITY_LIFETIME / 4)
					progressTillHit = 1f;

				Color c = DrawHelpers.MulticolorLerp(cos, voidColors);

				if (_dying)
					c = Color.Lerp(c, new Color(60, 0, 65, 0), progressTillHit);

				Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, c, 0f, starNonPreMult.Size() / 2f, scale, 0f, 0f);
				Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.White.Additive() * 0.75f, 0f, starNonPreMult.Size() / 2f, scale * 0.65f, 0f, 0f);

				if (_dying)
				{
					Main.spriteBatch.End();
					Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

					Main.spriteBatch.Draw(bloomNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.Black, 0f, bloomNonPreMult.Size() / 2f, x * 0.2f * progressTillHit, 0f, 0f);
					Main.spriteBatch.Draw(bloomNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.Black * 0.5f, 0f, bloomNonPreMult.Size() / 2f, x * 0.4f * progressTillHit, 0f, 0f);

					Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, new Color(60, 0, 65) * 0.6f, 0f, starNonPreMult.Size() / 2f, scale * 1.5f * progressTillHit, 0f, 0f);
					Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.Black * 0.4f, 0f, starNonPreMult.Size() / 2f, scale * 1.2f * progressTillHit, 0f, 0f);

					Main.spriteBatch.End();
					Main.spriteBatch.BeginDefault();
				}
			}

			if (_dying)
			{
				float progress = EaseFunction.EaseQuarticOut.Ease(1f - _pulseTimer / 30f);

				x = 0.2f * stacks;
				y = 0.1f * stacks;

				scale = new Vector2(x + 0.02f * sin, y + 0.02f * sin);

				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * (1f - progress) * 1.5f, 0f, star.Size() / 2f, scale * progress * Intensity, 0f, 0f);
				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * (1f - progress) * 0.75f, 0f, star.Size() / 2f, scale * 0.65f * progress * Intensity, 0f, 0f);

				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(60, 0, 65, 0) * (1f - progress) * 1.5f, 0f, star.Size() / 2f, scale * 0.5f * progress * Intensity, 0f, 0f);
				Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * (1f - progress) * 0.75f, 0f, star.Size() / 2f, scale * 0.25f * progress * Intensity, 0f, 0f);
			}

			return false;
		}
	}
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new VoidGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
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

		var main = Color.Lerp(new(225, 63, 255), new(166, 63, 255), sin);
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

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			DrawData item = input;
			item.position += offset;
			item.color = Color.Violet * 0.25f;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.color = Color.Black * 0.5f;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(Type);
			drawInfo.DrawDataCache.Add(item);
		}
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

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 normalized = velocity.SafeNormalize(Vector2.One);
		Vector2 pos = position + normalized * item.width;

		for (int i = 0; i < 2; i++)
		{
			Vector2 vel = normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(5f);

			ParticleHandler.SpawnParticle(new ImpactLine(pos, vel * 1.5f, Color.Purple * 0.5f, new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 40, 0.95f));

			if (Main.rand.NextBool(3))
				ParticleHandler.SpawnParticle(new VoidParticle(pos, vel, Color.Purple.Additive(), 0f, 0.2f, 65));
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (Main.rand.NextBool(45 + 40 * projectile.extraUpdates))
			ParticleHandler.SpawnParticle(new VoidParticle(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(1.5f), Color.Purple.Additive(), 0f, 0.3f, 65));

		if (Main.rand.NextBool(2 + 1 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), DustID.Granite, -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(4f), 150 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;
	}

	public class VoidGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
	{
		private Effect GetEffect => shader.Value;

		public override void Apply(Entity entity, DrawData? drawData = null)
		{
			if (!drawData.HasValue)
				return;

			GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
			GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
			GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

			GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
			GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["noiseCrystal"].Value);
			GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

			float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.01f));
			float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.015f));

			var main = Color.Lerp(new(225, 63, 255), new(166, 63, 255), sin);
			if (sin > 0.5f)
				main = Color.Lerp(main, Color.Black, sin);

			GetEffect.Parameters["uColor1"].SetValue(main.ToVector4() * 0.5f);
			GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(new(255, 63, 230), new(255, 63, 192), cos).ToVector4() * 0.5f);
			GetEffect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());

			GetEffect.Parameters["baseDepth"].SetValue(4f);
			GetEffect.Parameters["scale"].SetValue(0.66f);

			Apply();
		}
	}
}