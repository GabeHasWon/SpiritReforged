using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.ID;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Forest.Misc.ChairClub;

class BreakawayChairProj : BaseClubProj, IManualTrailProjectile
{
	public BreakawayChairProj() : base(new Vector2(30, 34)) { }

	public override float WindupTimeRatio => 0.8f;

	public static readonly SoundStyle Impact = new("SpiritReforged/Assets/SFX/Projectile/ChairBreak")
	{
		PitchRange = (0f, 0.5f),
		Volume = 0.5f,
		MaxInstances = 2
	};

	public override void SafeSetDefaults()
	{
		Projectile.penetrate = 1;
		Projectile.hostile = true;
		_parameters.HasIndicator = false;
	}

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 30 * MeleeSizeModifier;
		float trailWidth = 20 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = -MathHelper.PiOver4 / 2;

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = Color.White,
			SecondaryColor = Color.LightGray,
			TrailLength = 0.25f,
			Intensity = 0.5f,
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
	}

	public override void OnSwingStart() => TrailManager.ManualTrailSpawn(Projectile);

	public override bool CanHitPlayer(Player target) => target.whoAmI != Projectile.owner;
	internal override bool CanCollide(float progress) => false;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(Impact, Projectile.Center);

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

		if (target.isLikeATownNPC)
			EmoteHelper.SyncedEmote(target, 60, EmoteID.EmoteAnger);

		Projectile.Kill();
		Main.player[Projectile.owner].HeldItem.stack--; // "Break" a stack of the chair

		SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

		for (int i = 0; i < 3; ++i)
			Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Projectile.velocity, Mod.Find<ModGore>("BreakawayChair" + i).Type);
	}
}