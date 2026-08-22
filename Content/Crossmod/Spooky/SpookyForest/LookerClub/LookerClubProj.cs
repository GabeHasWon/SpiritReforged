using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.LookerClub;

class LookerClubProj : BaseClubProj
{
	class EyeballDust : ModDust
	{
		public override void OnSpawn(Dust dust) => UpdateType = DustID.Copper;
	}

	public LookerClubProj() : base(new Vector2(84, 90)) { }

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
		float trailWidth = 60 * MeleeSizeModifier;
		float angleRangeMod = 1.5f;
		float rotOffset = 0;

		if (FullCharge)
		{
			trailDist *= 1.1f;
			trailWidth *= 1.1f;
			angleRangeMod = 1.2f;
			rotOffset = -MathHelper.PiOver4 / 2;
		}

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = new Color(54, 9, 47),
			SecondaryColor = new Color(199, 7, 49),
			TrailLength = 0.33f,
			Intensity = 0.5f,
		};

		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
	}

	public override void OnSmash(Vector2 position)
	{
		TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		SoundEngine.PlaySound(SoundID.NPCHit1, position);

		DustClouds(5);

		float strength = FullCharge ? 1f : 0.75f;
		
		for (int i = 0; i < (FullCharge ? 15 : 10); i++)
		{
			Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(10, 10), DustID.Blood, -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 6f) * strength, 50, default, 1.25f).noGravity = true;

			Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(10, 10), DustID.Blood, -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 6f) * strength, 100, default, 2.25f);

			ParticleHandler.SpawnParticle(new SmokeCloud(position + Main.rand.NextVector2Circular(25, 25), -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 4f) * strength, Color.DarkRed * 0.25f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 60 + Main.rand.Next(30))
			{
				Pixellate = true,
				PixelDivisor = 2
			});
		}

		if (FullCharge && Main.myPlayer == Projectile.owner)
		{
			for (int k = 0; k < 2 + Main.rand.Next(2, 5); k++)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis("SpiritReforged: Looker Club Smash"), position - Vector2.UnitY * 30,
					Main.rand.NextVector2Circular(4f, 0.1f) - Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, 8f), ModContent.ProjectileType<EyeballMinion>(), Projectile.damage / 3, 1f, Projectile.owner);
			}

			for (int i = 0; i < Main.rand.Next(8, 15); i++)
			{
				Dust.NewDustPerfect(position - Vector2.UnitY * 30, ModContent.DustType<EyeballDust>(), Main.rand.NextVector2Circular(3f, 0.1f) - Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 4f), 200 + Main.rand.Next(30), default, Main.rand.NextFloat(1f, 2f));
			}
		}

		Vector2 shockwaveVector = Projectile.Bottom;

		if (FullCharge)
		{
			Projectile.frame = 1;

			float angle = MathHelper.PiOver4 * 1.5f;
			if (Projectile.direction > 0)
				angle = -angle + MathHelper.Pi;

			LookerShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.5f), 320, angle, 0.6f, new Color(206, 206, 206), Color.White);

			LookerShockwaveCircle(shockwaveVector - Vector2.UnitY * 12, 150, MathHelper.PiOver2, 0.5f, new Color(150, 12, 12), new Color(238, 45, 87));
		}
		else
			shockwaveVector -= Vector2.UnitY * 12; // was far in the ground when not fully charged

		LookerShockwaveCircle(shockwaveVector - Vector2.UnitY * 8, 250, MathHelper.PiOver2, 0.4f, new Color(150, 12, 12), new Color(238, 45, 87));
	}

	void LookerShockwaveCircle(Vector2 pos, float size, float xyRotation, float opacity, Color main, Color secondary)
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