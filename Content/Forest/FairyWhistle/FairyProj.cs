using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.FairyWhistle;

public class FairyProj : ModProjectile
{
	private ref float ColorStyle => ref Projectile.ai[0];

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 4;
		ProjectileID.Sets.TrailCacheLength[Type] = 15;
		ProjectileID.Sets.TrailingMode[Type] = 2;
		ProjectileID.Sets.MinionShot[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(10);
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.alpha = 255;
		Projectile.timeLeft = 120;
		Projectile.extraUpdates = 3;
		Projectile.scale = Main.rand.NextFloat(0.7f, 0.9f);
	}

	public override void AI()
	{
		if (Projectile.timeLeft > 20)
			Projectile.alpha = Math.Max(Projectile.alpha - 15, 0);
		else
			Projectile.alpha = Math.Min(Projectile.alpha + 15, 255);

		Projectile.UpdateFrame(6);
		Lighting.AddLight(Projectile.Center, Color.LimeGreen.ToVector3() / 3);
	}

	public override void OnKill(int timeLeft)
	{
		if (timeLeft <= 0)
			return;

		if (!Main.dedServ)
		{
			var velnormal = Vector2.Normalize(Projectile.velocity);
			velnormal *= 2;

			for (int i = 0; i < 3; i++) //weak burst of particles in direction of movement
				ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velnormal.RotatedByRandom(MathHelper.Pi / 8) * Main.rand.NextFloat(1f, 2f),
					FairyMinion.StyleColor(ColorStyle), Main.rand.NextFloat(0.5f, 0.6f), 40, 7, p => FairyMinion.RandomCurveParticleMovement(p, 0.12f, 0.95f)));

			for (int i = 0; i < 4; i++) //wide burst of slower moving particles in opposite direction
				ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, -velnormal.RotatedByRandom(MathHelper.Pi / 3) * Main.rand.NextFloat(0.25f, 1.5f),
					FairyMinion.StyleColor(ColorStyle), Main.rand.NextFloat(0.5f, 0.6f), 40, 7, p => FairyMinion.RandomCurveParticleMovement(p, 0.12f, 0.95f)));

			for (int i = 0; i < 3; i++) //narrow burst of faster, bigger particles
				ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velnormal.RotatedByRandom(MathHelper.Pi / 12) * Main.rand.NextFloat(1.5f, 3f),
					FairyMinion.StyleColor(ColorStyle), Main.rand.NextFloat(0.4f, 0.5f), 40, 7, p => FairyMinion.RandomCurveParticleMovement(p, 0.15f, 0.97f)));
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var color = FairyMinion.StyleColor(ColorStyle).Additive();
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, color * Projectile.Opacity, 0, bloom.Size() / 2, Projectile.scale * 0.15f, SpriteEffects.None, 0);
		
		Projectile.QuickDrawTrail(Main.spriteBatch, 0.4f, drawColor: color);
		Projectile.QuickDraw(Main.spriteBatch, drawColor: color);
		Projectile.QuickDraw(Main.spriteBatch, drawColor: Color.White.Additive());

		return false;
	}
}