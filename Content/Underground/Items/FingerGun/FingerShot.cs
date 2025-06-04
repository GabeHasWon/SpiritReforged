using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.FingerGun;

public class FingerShot : ModProjectile
{
	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.DamageType = DamageClass.Magic;
		Projectile.Size = new(4);
		Projectile.ignoreWater = true;
		Projectile.penetrate = 3;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.timeLeft = 120;
		Projectile.extraUpdates = 1;
		Projectile.friendly = true;
	}

	public override void AI()
	{
		Projectile.velocity *= 0.97f;
		Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
		Projectile.scale = Projectile.timeLeft / 120f;
		Projectile.scale = EaseFunction.EaseCircularOut.Ease(Projectile.scale);

		if (Main.dedServ)
			return;

		if(Main.rand.NextBool(14))
		{
			Vector2 velocity = Vector2.Normalize(Projectile.velocity) * 2;
			Color color = Color.Cyan * (Projectile.timeLeft / 120f);
			float scale = Main.rand.NextFloat(0.4f, 0.6f) * Projectile.scale;

			static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, color, scale, 14, 3, DelegateAction));
		}

		Lighting.AddLight(Projectile.Center, Color.LightCyan.ToVector3() / 3);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.dedServ)
			return;

		Color particleColor = Color.Lerp(Color.LightCyan, Color.Cyan, 0.66f).Additive();
		ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, Main.rand.NextFloatDirection(), particleColor, 0.33f * Projectile.scale, 14));
		ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center, Vector2.Zero, particleColor, new Vector2(0.5f, 1) * Projectile.scale, 9, 0, target));
		SoundEngine.PlaySound(SoundID.Item158.WithPitchOffset(1).WithVolumeScale(0.33f), Projectile.Center);
	}

	public override void OnKill(int timeLeft)
	{
		if (timeLeft == 0 || Main.dedServ || Projectile.penetrate == 0)
			return;

		Color particleColor = Color.Lerp(Color.LightCyan, Color.Cyan, 0.66f).Additive();
		ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, Main.rand.NextFloatDirection(), particleColor, 0.33f * Projectile.scale, 14));
		ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center, Vector2.Zero, particleColor, new Vector2(0.5f, 1) * Projectile.scale, 9, 0));

		SoundEngine.PlaySound(SoundID.Item158.WithPitchOffset(1).WithVolumeScale(0.5f), Projectile.Center);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float opacity = Projectile.timeLeft / 120f;
		opacity = EaseFunction.EaseQuadOut.Ease(opacity);

		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.Cyan.Additive() * 0.66f * opacity, 0, bloom.Size() / 2, 0.15f * Projectile.scale, SpriteEffects.None);

		Projectile.QuickDrawTrail(baseOpacity : opacity * 0.75f, drawColor: Color.DarkCyan.Additive() * opacity);
		Projectile.QuickDraw(drawColor: Color.White.Additive() * opacity); 
		
		return false;
	}
}