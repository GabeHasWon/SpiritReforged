using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Survivalist;
public class SurvivalistGrenadeProjectile : ModProjectile
{
	public override void SetDefaults()
	{
		Projectile.Size = new(8);

		Projectile.friendly = true;
		Projectile.timeLeft = 600;
		Projectile.penetrate = 1;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation();

		if (++Projectile.ai[0] > 15)
		{
			Projectile.velocity *= 0.985f;
			Projectile.velocity.Y += 0.05f;
			if (Projectile.velocity.Y > 0)
				Projectile.velocity.Y *= 1.05f;

			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
		}	
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Explode();
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Explode();

		return true;
	}

	void Explode()
	{
		float strength = Main.rand.NextFloat(0.9f, 1.33f);

		if (Main.myPlayer == Projectile.owner)
			ScreenshakeHelper.Shake(Projectile.Center, -Projectile.velocity * 0.25f, 1.5f, 2, 10);

		SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);

		Vector2 glowPos = Projectile.Center;
		Vector2 stretch = Vector2.One;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.Yellow.Additive(), Color.Orange, 0.6f, 180 * strength, 30, "Smoke", stretch, EaseFunction.EaseQuinticOut)
		{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.White.Additive(), Color.Orange.Additive(), 0.3f, 120 * strength, 30, "Smoke", stretch, EaseFunction.EaseCubicOut)
		{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

		for (int i = 0; i < (int)(8 * strength); i++)
		{
			Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(16f, 16f);
			Vector2 velocity = Main.rand.NextVector2CircularEdge(13f, 13f) * Main.rand.NextFloat(0.5f, 1f) * strength;

			ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.Orange, Color.Goldenrod, Main.rand.NextFloat()), 1.35f, Main.rand.Next(10, 30), p => p.Velocity *= 0.9f, tileCollide: false));
		}

		Main.NewText("BOOM");
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;

		Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0f, 0f);

		return false;
	}
}
