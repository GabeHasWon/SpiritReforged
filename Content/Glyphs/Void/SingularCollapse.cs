using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Void;

public class SingularCollapse : ModProjectile
{
	public const int LINGER_TIME = 900;
	public const int PULSE_TIME = 180;
	public const int PULSE_COUNT = 5;

	public override string Texture => AssetLoader.EmptyTexture;

	public int TargetIndex
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	public int Stacks
	{
		get => (int)Projectile.ai[1];
		set
		{
			_timeSinceStack = 0;
			Projectile.ai[1] = value;
		}
	}

	public float Progress => 1f - Projectile.timeLeft / (float)PULSE_TIME;

	public bool dying;

	private float _timeSinceStack;
	private float _intensity;
	private float _visualProgress;
	private SingularityRenderSystem.ShaderItem _shaderItem;

	public int GetPulseTime()
	{
		int pulseDuration = (int)((PULSE_TIME - PULSE_TIME / 4) / (float)PULSE_COUNT);
		return Projectile.timeLeft % pulseDuration;
	}

	public bool TryGetTarget(out NPC target)
	{
		if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs)
		{
			target = null;
			return false;
		}

		target = Main.npc[TargetIndex];
		return target.active;
	}

	public override void Load()
	{
		if (!Main.dedServ)
		{
			Asset<Effect> shader = ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/VoidGlyphSingularity");
			Filters.Scene["SpiritReforged:VoidGlyphSingularity"] = new Filter(new ScreenShaderData(shader, "ScreenPass"), EffectPriority.VeryHigh);
		}
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(100);
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = LINGER_TIME;
		Projectile.DamageType = DamageClass.Generic;
		Projectile.damage = 5;
		Projectile.friendly = true;
		Projectile.penetrate = -1;

		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 15;
	}

	public override bool? CanDamage() => dying && Progress > 0.15f && GetPulseTime() == 0;

	public override void AI()
	{
		const int decay_start = 300; //The number of ticks it takes before Stacks start decaying per decay_rate
		const int decay_rate = 60; //The number of ticks it takes for stack to decay after decay_start

		if (dying)
		{
			Projectile.Center += Main.rand.NextVector2Circular(2.5f, 2.5f) * GetPulseTime() / 30f;
			Projectile.velocity *= 0.97f;

			if (Progress < 0.15f)
			{
				float interpolant = Progress / 0.15f;

				_visualProgress = MathHelper.Lerp(0, 0.5f, EaseFunction.EaseQuinticIn.Ease(interpolant));
				_intensity = interpolant;
			}
			else if (Progress > 0.9f)
			{
				float interpolant = (Progress - 0.9f) / 0.1f;

				_visualProgress = MathHelper.Lerp(0.35f, 0f, EaseFunction.EaseQuinticOut.Ease(interpolant));
				_intensity = MathHelper.Lerp(0.7f, 0f, EaseFunction.EaseQuinticOut.Ease(interpolant));
			}
			else if (!Main.dedServ)
			{
				if (Main.rand.NextBool())
				{
					Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(75f, 75f) * _intensity;
					Vector2 velocity = pos.DirectionTo(Projectile.Center) * 3f;

					Dust.NewDustPerfect(pos, DustID.Granite, velocity, 230, default, Main.rand.NextFloat(1f, 1.5f)).noGravity = true;
				}

				if (Main.rand.NextBool(9))
				{
					Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(90f, 90f) * _intensity;
					Vector2 velocity = pos.DirectionTo(Projectile.Center) * 3f;

					ParticleHandler.SpawnParticle(new VoidParticle(pos, velocity, Color.Purple.Additive(), 0f, Main.rand.NextFloat(0.15f, 0.3f) * _intensity, 45));
				}

				if (Projectile.timeLeft > 0 && GetPulseTime() == 0)
				{
					_visualProgress -= 0.15f / PULSE_COUNT;
					_intensity -= 0.3f / PULSE_COUNT;

					for (int i = 0; i < 4; i++)
					{
						Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
						float rotation = Main.rand.NextFloat(6.28f);
						ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center, velocity, Color.Purple.Additive(), 0.2f, 35, 0, DecelerateAction)
						{ Rotation = rotation });

						ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center, velocity, Color.LightPink.Additive(), 0.1f, 35, 0, DecelerateAction)
						{ Rotation = rotation });

						velocity = Main.rand.NextVector2Circular(8f, 0.5f).RotatedByRandom(0.3f);

						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.Purple.Additive(), 0.5f, 40, 3, DecelerateAction));
						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.LightPink.Additive(), 0.3f, 40, 3, DecelerateAction));

						ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Main.rand.NextVector2CircularEdge(9f, 9f) * Main.rand.NextFloat(0.9f, 1.1f), Color.Purple * 0.5f, new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 60, 0.9f));
						ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Main.rand.NextVector2CircularEdge(9f, 9f) * Main.rand.NextFloat(0.9f, 1.1f), Color.Black * 0.5f, new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 60, 0.9f));
					}

					SoundEngine.PlaySound(Main.rand.NextBool() ? VoidGlyph.VoidHit1 : VoidGlyph.VoidHit2, Projectile.Center);
				}
			}
		}
		else
		{
			if (TryGetTarget(out NPC target))
			{
				Projectile.Center = target.Center;
				Projectile.ArmorPenetration = Stacks;
			}
			else
			{
				StartDying();
			}

			if (Projectile.timeLeft <= 2 || Stacks >= VoidNPC.MAX_STACKS)
			{
				StartDying();
			}
		}

		if (!Main.dedServ)
		{
			Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * (Stacks / (float)VoidNPC.MAX_STACKS));

			if (_shaderItem != null)
			{
				//Update the shader item
				_shaderItem.Position = Projectile.Center;
				_shaderItem.Scale = 0.8f + Stacks / (float)VoidNPC.MAX_STACKS * 0.75f;
				_shaderItem.Progress = _visualProgress;
				_shaderItem.Intensity = _intensity;
				_shaderItem.timeActive = 2;
			}
		}

		if (++_timeSinceStack > decay_start && (_timeSinceStack - decay_start) % decay_rate == 0 && Stacks > 0) //Remove stacks over time
		{
			int oldTimeSince = (int)_timeSinceStack;
			Stacks--;
			_timeSinceStack = oldTimeSince;

			Projectile.netUpdate = true;
		}

		static void DecelerateAction(Particle p)
		{
			p.Velocity *= 0.95f;
			p.Rotation += p.Velocity.Length() * 0.1f;
		}
	}

	private void StartDying()
	{
		if (dying)
			return;

		SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 3f, Pitch = -0.5f }, Projectile.Center);

		Projectile.timeLeft = PULSE_TIME;
		Projectile.damage += 5;
		Projectile.netUpdate = true;

		if (TryGetTarget(out NPC target))
		{
			Projectile.velocity = target.velocity * 0.1f;

			int size = 100 + Stacks * 10;
			Projectile.Resize(size, size);
		}

		if (!Main.dedServ)
			SingularityRenderSystem.ShaderItems.Add(_shaderItem = new());

		dying = true;
	}

	public override void OnKill(int timeLeft)
	{
		if (dying)
			SoundEngine.PlaySound((Main.rand.NextBool() ? VoidGlyph.VoidHit1 : VoidGlyph.VoidHit2) with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);
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
		SingularityHitEffects(target, damageDone, hit.Crit);

		if (Main.netMode != NetmodeID.SinglePlayer)
			MultiplayerLoader.Send(nameof(SingularityHitEffects), -1, -1, target, damageDone, hit.Crit);
	}

	[NetSynced(true)]
	public static void SingularityHitEffects(NPC target, int damage, bool crit)
	{
		if (!Main.dedServ)
		{
			Rectangle hitbox = target.Hitbox;
			int index = CombatText.NewText(hitbox, Color.White, damage, crit);
			ColoredCombatText.AddCombatText(index, Color.Purple, Color.DarkViolet);
		}

		if (target.TryGetGlobalNPC(out VoidNPC gnpc))
			gnpc.defenseReductionTimer = 300;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadProjectile(79);

		if (Stacks < 0)
			return false;

		Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
		Texture2D starNonPreMult = TextureAssets.Projectile[79].Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Texture2D bloomNonPreMult = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float sin = (float)Math.Sin(Projectile.timeLeft);
		float cos = Math.Abs((float)Math.Sin(Projectile.timeLeft * 0.02f));

		float x = 1f + 0.15f * Stacks;
		float y = 0.3f + 0.05f * Stacks;

		Vector2 scale = new(x + 0.02f * sin, y + 0.02f * sin);
		Vector2 offset = Vector2.Zero;

		if (Projectile.timeLeft < 60 && dying)
			scale *= Projectile.timeLeft / 60f;

		if (dying)
		{
			float progress = Progress * 2f;
			offset = Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * progress;

			Color c = new(60, 0, 65, 0);

			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.4f * progress * _intensity, 0f, 0f);
			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.3f * progress * _intensity, 0f, 0f);
			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, c * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.3f * progress * _intensity, 0f, 0f);
			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * 0.5f, 0f, bloom.Size() / 2f, scale.X * 0.25f * progress * _intensity, 0f, 0f);

			float prog = 1f - Projectile.timeLeft / (float)PULSE_TIME;

			if (prog < 0.3f)
			{
				float progressTillHit = 1f - prog / 0.3f;
				scale *= EaseFunction.EaseQuinticOut.Ease(progressTillHit);
			}
			else
			{
				scale *= 0f;
			}
		}

		Color[] voidColors = [new(255, 65, 255, 0), new(255, 65, 185, 0), new(211, 65, 255, 0), new(166, 65, 255, 0)];

		if (_timeSinceStack < 60)
		{
			float progress = EaseFunction.EaseQuarticInOut.Ease(1f - _timeSinceStack / 60f);
			float starScale = 0.06f + 0.03f * Stacks;
			float rotation = (Progress + Stacks) * (60f - _timeSinceStack) * 0.001f;

			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * progress, rotation, star.Size() / 2f, starScale * progress, 0f, 0f);
			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * progress * 0.75f, rotation, star.Size() / 2f, starScale * 0.66f * progress, 0f, 0f);
		}

		if (scale.LengthSquared() > 0f)
		{
			float progressTillHit = Progress * 2f;
			if (Projectile.timeLeft < PULSE_TIME - PULSE_TIME / 4)
				progressTillHit = 1f;

			Color multicolorLerp = DrawHelpers.MulticolorLerp(cos, voidColors);

			if (dying)
				multicolorLerp = Color.Lerp(multicolorLerp, new Color(60, 0, 65, 0), progressTillHit);

			Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, multicolorLerp, 0f, starNonPreMult.Size() / 2f, scale, 0f, 0f);
			Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.White.Additive() * 0.75f, 0f, starNonPreMult.Size() / 2f, scale * 0.65f, 0f, 0f);

			if (dying)
			{
				Main.spriteBatch.End(); //BATCH ME
				Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

				Main.spriteBatch.Draw(bloomNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.Black, 0f, bloomNonPreMult.Size() / 2f, x * 0.2f * progressTillHit, 0f, 0f);
				Main.spriteBatch.Draw(bloomNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.Black * 0.5f, 0f, bloomNonPreMult.Size() / 2f, x * 0.4f * progressTillHit, 0f, 0f);

				Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, new Color(60, 0, 65) * 0.6f, 0f, starNonPreMult.Size() / 2f, scale * 1.5f * progressTillHit, 0f, 0f);
				Main.spriteBatch.Draw(starNonPreMult, Projectile.Center + offset - Main.screenPosition, null, Color.Black * 0.4f, 0f, starNonPreMult.Size() / 2f, scale * 1.2f * progressTillHit, 0f, 0f);

				Main.spriteBatch.End();
				Main.spriteBatch.BeginDefault();
			}
		}

		if (dying)
		{
			float progress = EaseFunction.EaseQuarticOut.Ease(1f - GetPulseTime() / 30f);

			x = 0.2f * Stacks;
			y = 0.1f * Stacks;

			scale = new Vector2(x + 0.02f * sin, y + 0.02f * sin);

			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(255, 65, 255, 0) * (1f - progress) * 1.5f, 0f, star.Size() / 2f, scale * progress * _intensity, 0f, 0f);
			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * (1f - progress) * 0.75f, 0f, star.Size() / 2f, scale * 0.65f * progress * _intensity, 0f, 0f);

			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, new Color(60, 0, 65, 0) * (1f - progress) * 1.5f, 0f, star.Size() / 2f, scale * 0.5f * progress * _intensity, 0f, 0f);
			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * (1f - progress) * 0.75f, 0f, star.Size() / 2f, scale * 0.25f * progress * _intensity, 0f, 0f);
		}

		return false;
	}
}