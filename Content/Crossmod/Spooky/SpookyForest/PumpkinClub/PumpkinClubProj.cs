using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.PumpkinClub;

class PumpkinClubProj : BaseClubProj
{
	public PumpkinClubProj() : base(new Vector2(94, 88)) { }

	public override float WindupTimeRatio => 0.8f;

	public override void SafeSetStaticDefaults() => Main.projFrames[Type] = 2;

	public override void OnSwingStart()
	{
		if (!Main.dedServ)
			CreateTrail(TrailSystem.ProjectileRenderer);

		Projectile.frame = 0;
	}

	public void CreateTrail(ProjectileTrailRenderer renderer)
	{
		float trailDist = 65 * MeleeSizeModifier;
		float trailWidth = 70 * MeleeSizeModifier;
		float angleRangeMod = 1.25f;
		float rotOffset = 0;

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = Color.LightYellow,
			SecondaryColor = Color.Orange,
			TrailLength = 0.2f,
			Intensity = 0.3f,
		};

		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));

		if (FullCharge)
		{
			trailDist *= 1.25f;
			trailWidth *= 1.5f;

			parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
			{
				Color = Color.Orange,
				SecondaryColor = Color.DarkOrange.Additive(),
				TrailLength = 1f,
				Intensity = 1f,
			};

			renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, s => SwingTrail.NoiseSwingShaderParams(s, "EnergyTrail", new Vector2(0.8f, 0.4f))));
			
			trailDist *= 0.95f;
			trailWidth *= 0.65f;

			parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
			{
				Color = Color.Yellow.Additive(),
				SecondaryColor = Color.Goldenrod.Additive(),
				TrailLength = 0.9f,
				Intensity = 0.33f,
			};

			renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, s => SwingTrail.NoiseSwingShaderParams(s, "EnergyTrail", new Vector2(0.9f, 0.4f))));
		}
	}

	public override void OnSmash(Vector2 position)
	{
		TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		SoundEngine.PlaySound(SoundID.NPCHit1, position);

		DustClouds(9);

		float strength = FullCharge ? 1f : 0.75f;

		for (int i = 0; i < (FullCharge ? 15 : 10); i++)
		{
			Vector2 velocity = Vector2.UnitX.RotatedByRandom(0.3f) * Main.rand.NextFloat(2f) * Projectile.direction - Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(1.2f);

			velocity *= strength;

			Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(10, 10), DustID.Torch,
				velocity * Main.rand.NextFloat(15f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;

			Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(10, 10), DustID.Torch,
				velocity * Main.rand.NextFloat(5f), 0, default, Main.rand.NextFloat(1.5f));

			Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(10, 10), DustID.Pumpkin,
				Main.rand.NextVector2Circular(12, 12) * strength, 70, default, Main.rand.NextFloat(3f)).noGravity = true;
		}

		if (FullCharge)
		{
			for (int i = 0; i < 5; i++)
			{
				Vector2 pos = position + Main.rand.NextVector2Circular(25, 25) - Vector2.UnitY * 30;
				Vector2 velocity = Vector2.UnitX.RotatedByRandom(0.3f) * Main.rand.NextFloat(5, 7) * Projectile.direction - Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(2f);

				Projectile.NewProjectile(Projectile.GetSource_FromThis("SpiritReforged: Pumpkin Club Smash"), pos,
					velocity, ModContent.ProjectileType<PumpkinEmberProjectile>(), Projectile.damage / 3, Projectile.knockBack / 5, Projectile.owner);
			}

			Projectile.frame = 1;

			for (int i = 1; i <= 10; i++)
			{
				Vector2 pos = position + Main.rand.NextVector2Circular(25, 25) - Vector2.UnitY * 30;
				Vector2 velocity = Vector2.UnitX.RotatedByRandom(0.3f) * Main.rand.NextFloat(9) * Projectile.direction - Vector2.UnitY.RotatedByRandom(0.7f) * Main.rand.NextFloat(8f);

				var g = Gore.NewGorePerfect(Projectile.GetSource_FromThis("SpiritReforged: Pumpkin Club Smash"), pos,
					velocity, Mod.Find<ModGore>("PumpkinClubGore_0" + i).Type, Main.rand.NextFloat(0.9f, 1.2f));

				g.timeLeft = Main.rand.Next(120, 240);
				g.rotation = Main.rand.NextFloat(6.28f);
				g.behindTiles = Main.rand.NextBool(3);
				g.sticky = !Main.rand.NextBool(5);
			}

			float angle = MathHelper.PiOver4 * 1.5f;
			if (Projectile.direction > 0)
				angle = -angle + MathHelper.Pi;

			PumpkinShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.1f) - Vector2.UnitY * 16, 240, angle, 1f, Color.Orange.Additive(), Color.DarkOrange);

			PumpkinShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.08f) - Vector2.UnitY * 20, 180, angle, 0.9f, Color.Orange.Additive(), Color.OrangeRed);
		}
		else
		{
			Vector2 shockwaveVector = Projectile.Bottom - Vector2.UnitY * 12;

			PumpkinShockwaveCircle(shockwaveVector - Vector2.UnitY * 8, 250, MathHelper.PiOver2, 0.4f, Color.Orange.Additive(), Color.Goldenrod);
		}		
	}

	void PumpkinShockwaveCircle(Vector2 pos, float size, float xyRotation, float opacity, Color main, Color secondary)
	{
		var easeFunction = EaseBuilder.EaseCubicOut;
		float ringWidth = 0.4f;
		int lifetime = 55;
		float zRotation = 0.9f;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(
			pos,
			main * opacity,
			secondary * opacity,
			ringWidth,
			size * TotalScale,
			lifetime,
			"supPerlin",
			new Vector2(2, 3),
			easeFunction).WithSkew(zRotation, xyRotation).UsesLightColor());
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		var basePosition = Vector2.Lerp(Projectile.Center, target.Center, 0.6f);
		Vector2 directionUnit = basePosition.DirectionFrom(Owner.MountedCenter) * TotalScale;

		int numParticles = FullCharge ? 12 : 8;
		for (int i = 0; i < numParticles; i++)
		{
			float maxOffset = 15;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 position = basePosition + directionUnit.RotatedBy(MathHelper.PiOver2) * offset;
			float velocity = MathHelper.Lerp(12, 2, Math.Abs(offset) / maxOffset) * Main.rand.NextFloat(0.9f, 1.1f);
			if (FullCharge)
				velocity *= 1.5f;

			float rotationOffset = MathHelper.PiOver4 * offset / maxOffset;
			rotationOffset *= Main.rand.NextFloat(0.9f, 1.1f);

			Vector2 particleVel = directionUnit.RotatedBy(rotationOffset) * velocity;
			var p = new ImpactLine(position, particleVel, Color.White * 0.5f, new Vector2(0.15f, 0.6f) * TotalScale, Main.rand.Next(15, 20), 0.8f);
			p.UseLightColor = true;
			ParticleHandler.SpawnParticle(p);

			if (!Main.rand.NextBool(3))
				Dust.NewDustPerfect(position, DustID.t_LivingWood, particleVel / 3, Scale: 0.5f);
		}

		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 3, Color.LightGray, 0.06f * TotalScale, EaseFunction.EaseCubicOut, 30));
		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 6, Color.LightGray, 0.08f * TotalScale, EaseFunction.EaseCubicOut, 30));
	}
}

class PumpkinEmberProjectile : ModProjectile, IDrawPixelated
{
	private VertexTrail[] _trails;
	private static int MAX_TIMELEFT = 600;

	public bool HitTile
	{
		get => Projectile.ai[1] == 1;
		set => Projectile.ai[1] = value ? 1 : 0;
	}

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults()
	{
		Projectile.Size = new(8);
		Projectile.tileCollide = true;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = 10;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 25;
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.extraUpdates = 1;

		Projectile.scale = Main.rand.NextFloat(0.9f, 1.5f);
	}

	public override bool PreAI()
	{
		if (HitTile)
		{
			if (Projectile.ai[2] < 10)
				Projectile.ai[2]++;

			Projectile.timeLeft--;

			float progress = Projectile.timeLeft / (float)MAX_TIMELEFT;

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
				DustID.Torch, Main.rand.NextVector2Circular(1.5f, 1.5f) * progress, 0, default, Main.rand.NextFloat(2.85f) * progress * Projectile.scale).noGravity = true;

			if (Main.rand.NextBool(20))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
					DustID.Torch, Main.rand.NextVector2Circular(2.5f, 2.5f) * progress, 0, default, Main.rand.NextFloat(1.45f) * progress * Projectile.scale).noGravity = false;

			return false;
		}

		return true;
	}

	public override void AI()
	{
		if (!Main.dedServ && _trails == null)
			CreateTrail();

		if (_trails is not null)
		{
			foreach (VertexTrail trail in _trails)
			{
				trail.Update();
			}
		}

		if (++Projectile.ai[0] > 20)
		{
			Projectile.velocity.Y += 0.04f;
			Projectile.velocity.X *= 0.99f;
		}

		Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.Torch, Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(1.5f) * Projectile.scale).noGravity = true;
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (!HitTile)
		{
			HitTile = true;
			Projectile.position += oldVelocity;
			Projectile.velocity *= 0;
		}

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(BuffID.OnFire, 240);
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(Projectile);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(Color.Red, Color.DarkOrange.Additive(), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 19 * Projectile.scale, 40 * Projectile.scale),
			new VertexTrail(new GradientTrail(Color.Goldenrod, Color.Yellow.Additive(), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 14 * Projectile.scale, 40 * Projectile.scale),
		];
	}

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch sb)
	{
		if (_trails != null)
			foreach (VertexTrail trail in _trails)
			{
				if (Projectile.ai[2] > 0)
				{
					trail.Opacity = 1f - Projectile.ai[2] / 10f;
					trail.WidthMultiplier = 1f - Projectile.ai[2] / 10f;
				}			
				else if (Projectile.timeLeft < 120)
					trail.Opacity = Projectile.timeLeft / 120f;

				trail?.Draw(TrailSystem.TrailShaders, sb.GraphicsDevice, Matrix.Identity);
			}

		Texture2D tex = AssetLoader.LoadedTextures["Bloom"].Value;
		float progress = Projectile.timeLeft / (float)MAX_TIMELEFT;

		Vector2 position = Projectile.Center - Main.screenPosition;
		IDrawPixelated.PixelateDrawPosition(ref position);

		Main.spriteBatch.Draw(tex, position, null, Color.OrangeRed.Additive() * 0.35f * progress, 0, tex.Size() / 2, 0.035f * Projectile.scale, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(tex, position, null, Color.Orange.Additive() * 0.35f * progress, 0, tex.Size() / 2, 0.05f * Projectile.scale, SpriteEffects.None, 0);

		Main.spriteBatch.Draw(tex, position, null, Color.Goldenrod.Additive() * 0.09f * progress, 0, tex.Size() / 2, 0.09f * Projectile.scale, SpriteEffects.None, 0);
	}
}