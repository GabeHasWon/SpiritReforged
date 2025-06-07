using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Misc.ChairClub;

class BreakawayChairProj : BaseClubProj, IManualTrailProjectile
{
	public BreakawayChairProj() : base(new Vector2(24)) 
	{	
	}

	public override float WindupTimeRatio => 0.8f;

	public override void SafeSetDefaults()
	{
		Projectile.penetrate = 1;
		Projectile.hostile = true;

		_parameters.PlayFullChargeSound = false;
		_parameters.HasSmash = false;
		_parameters.HasChargeFlash = false;
	}

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 26 * MeleeSizeModifier;
		float trailWidth = 8 * MeleeSizeModifier;
		float angleRangeMod = 1f;
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
			Color = Color.White,
			SecondaryColor = Color.LightGray,
			TrailLength = 0.33f,
			Intensity = 0.5f,
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
	}

	public override void OnSwingStart() => TrailManager.ManualTrailSpawn(Projectile);

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

		Projectile.Kill();

		SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

		// "Break" a stack of the chair
		Player owner = Main.player[Projectile.owner];
		owner.HeldItem.stack--;

		for (int i = 0; i < 3; ++i)
			Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Projectile.velocity, ModContent.Find<ModGore>("SpiritReforged/BreakawayChair" + i).Type);
	}
}