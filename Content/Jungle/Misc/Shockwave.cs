using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Jungle.Misc;

public class Shockwave : ModProjectile
{
	private bool _justSpawned;

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(60);
		Projectile.penetrate = -1;
		Projectile.friendly = true;
		Projectile.hide = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.timeLeft = 30;

		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void AI()
	{
		Projectile.velocity *= .94f;
		Projectile.direction = (Projectile.velocity.X < 0) ? -1 : 1;

		if (_justSpawned)
			return;

		DustClouds(10);

		for (int i = 0; i < 5; i++)
		{
			float mag = (i == 0) ? 1 : Main.rand.NextFloat();
			var unit = RandomVel((1f - mag) * -1.5f);
			var velocity = unit * Main.rand.NextFloat(20f, 30f) * Math.Max(mag, .5f);

			var p = new ImpactLine(Projectile.Center, velocity, Color.White * 0.5f, new Vector2(1, mag * 2) * Projectile.scale * 2, Main.rand.Next(10, 15), 0.85f);
			p.UseLightColor = true;
			ParticleHandler.SpawnParticle(p);
		}

		for (int i = 0; i < 12; i++)
			Dust.NewDustPerfect(Projectile.Center, DustID.t_LivingWood, RandomVel(-Main.rand.NextFloat()) * Main.rand.NextFloat(2f, 8f), Scale: Main.rand.NextFloat(0.5f, 1.1f)).noGravity = Main.rand.NextBool();

		_justSpawned = true;

		Vector2 RandomVel(float dir) => Vector2.Normalize(Projectile.velocity).RotatedBy(dir * Projectile.direction);
	}

	private void DustClouds(int maxClouds)
	{
		for (int i = 0; i < maxClouds; i++)
		{
			Vector2 smokePos = Projectile.Bottom + Vector2.UnitX * Main.rand.NextFloat(-20, 20);

			float scale = Main.rand.NextFloat(0.05f, 0.07f) * Projectile.scale * 2;

			float speed = Main.rand.NextFloat(9, 15);
			int lifeTime = Main.rand.Next(20, 30);
			var velocity = (Vector2.Normalize(Projectile.velocity) * speed).RotatedByRandom(1) - new Vector2(0, 1);

			ParticleHandler.SpawnParticle(new SmokeCloud(smokePos, velocity, Color.DarkGray * 0.5f, scale * 1.5f, Common.Easing.EaseFunction.EaseCubicOut, lifeTime));
			ParticleHandler.SpawnParticle(new SmokeCloud(smokePos, velocity, Color.LightGray, scale, Common.Easing.EaseFunction.EaseCubicOut, lifeTime));
		}
	}
}