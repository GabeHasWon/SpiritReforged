using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.FingerGun;

public class FingerShot : ModProjectile
{
	private bool PopulatedOldPositions
	{
		get => Projectile.ai[0] == 1;
		set => Projectile.ai[0] = value ? 1 : 0;
	}

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.DamageType = DamageClass.Magic;
		Projectile.Size = new(4);
		Projectile.ignoreWater = true;
		Projectile.penetrate = 3;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.timeLeft = 60;
		Projectile.extraUpdates = 0;
		Projectile.friendly = true;
	}

	public override void AI()
	{
		if (!PopulatedOldPositions)
		{
			for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
				Projectile.oldPos[i] = Projectile.position;

			PopulatedOldPositions = true;
		}

		Projectile.velocity *= 0.93f;
		Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
		Projectile.scale = 0.5f * Projectile.timeLeft / 60f;
		Projectile.scale = EaseFunction.EaseCircularOut.Ease(Projectile.scale);

		if (Main.dedServ)
			return;

		if (Main.rand.NextBool(8))
		{
			Vector2 velocity = Vector2.Normalize(Projectile.velocity) * 2 * (Projectile.timeLeft / 60f);
			Color color = Color.DarkCyan * (Projectile.timeLeft / 60f);
			float scale = Main.rand.NextFloat(0.3f, 0.7f) * Projectile.scale;

			static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, color.Additive(), scale, 18, 3, DelegateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.White.Additive() * (Projectile.timeLeft / 60f), scale / 3, 18, 3, DelegateAction));
		}

		Lighting.AddLight(Projectile.Center, Color.LightCyan.ToVector3() / 3);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => DoFX();

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		DoFX();
		return base.OnTileCollide(oldVelocity);
	}

	private void DoFX()
	{
		if (Main.dedServ)
			return;

		Color particleColor = Color.Lerp(Color.LightCyan, Color.Cyan, 0.66f).Additive();
		ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, Main.rand.NextFloatDirection(), particleColor, 0.33f * Projectile.scale, 14));
		ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center, Vector2.Zero, particleColor, new Vector2(0.5f, 1.25f) * Projectile.scale, 9, 0));

		for(int i = 0; i < 7; i++)
		{
			ParticleHandler.SpawnParticle(new StarParticle(Projectile.Center, Main.rand.NextVector2Circular(1.5f, 1.5f), Color.LightCyan.Additive(), Color.Cyan.Additive(), 0.1f, 14, 2));
		}

		SoundEngine.PlaySound(SoundID.Item158.WithPitchOffset(1).WithVolumeScale(0.5f), Projectile.Center);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Type].Value;

		float opacity = Projectile.timeLeft / 120f;
		opacity = EaseFunction.EaseQuadOut.Ease(opacity);

		float iterations = 40;
		for(int i = (int)iterations; i > 0; i--)
		{
			float progress = i / iterations;
			var position = Vector2.Lerp(Projectile.position, Projectile.oldPos[ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1], progress);
			var color = Color.Lerp(Color.LightCyan.Additive(), Color.DarkCyan.Additive(), EaseFunction.EaseCircularOut.Ease(progress)) * (1 - progress);

			Main.EntitySpriteDraw(projTex, position - Main.screenPosition, null, Projectile.GetAlpha(color) * opacity * (1 - progress), Projectile.rotation, projTex.Size() / 2, Projectile.scale, SpriteEffects.None);
		}
		
		return false;
	}
}