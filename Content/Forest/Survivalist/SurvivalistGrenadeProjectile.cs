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

		if (++Projectile.ai[0] > 30)
		{
			Projectile.velocity *= 0.985f;
			Projectile.velocity.Y += 0.04f;
			if (Projectile.velocity.Y > 0)
				Projectile.velocity.Y *= 1.07f;

			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(BuffID.OnFire, 300);

		if (Projectile.penetrate > 0)
			Explode();
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.penetrate > 0)
			Explode();

		return false;
	}

	void Explode()
	{
		Projectile.damage *= 3;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 10;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 10;
		Projectile.Resize(150, 150);

		float strength = Main.rand.NextFloat(0.9f, 1.33f);

		if (Main.myPlayer == Projectile.owner)
			ScreenshakeHelper.Shake(Projectile.Center, -Projectile.velocity * 0.25f, 1.5f, 2, 10);

		SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);

		for (int i = 0; i < (int)(12 * strength); i++)
		{
			ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center + Main.rand.NextVector2Circular(32, 32), Main.rand.NextVector2Circular(3.5f, 3.5f),
				new Color(50, 50, 50), 20 + Main.rand.Next(40), false, false, p =>
				{
					p.Velocity *= 0.9f;
					p.Velocity.Y -= 0.03f;
				})
			{ Layer = ParticleLayer.BelowProjectile});

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center + Main.rand.NextVector2Circular(32, 32), Main.rand.NextVector2Circular(3.5f, 3.5f),
				new Color(150, 150, 150), 10 + Main.rand.Next(40), false, false, p =>
				{
					p.Velocity *= 0.9f;
					p.Velocity.Y -= 0.03f;
				})
			{ Layer = ParticleLayer.BelowProjectile });

			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(32, 32), Main.rand.NextVector2Circular(5.5f, 5.5f),
				new Color(100, 100, 100) * 0.2f, 0.2f, EaseFunction.EaseQuadOut, Main.rand.Next(50)));
		}

		Vector2 glowPos = Projectile.Center;
		Vector2 stretch = Vector2.One;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.Yellow.Additive(), Color.Orange, 0.6f, 180 * strength, 30, "Smoke", stretch, EaseFunction.EaseQuinticOut)
		{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.White.Additive(), Color.Orange.Additive(), 0.3f, 120 * strength, 30, "Smoke", stretch, EaseFunction.EaseCubicOut)
		{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

		for (int i = 0; i < (int)(8 * strength); i++)
		{
			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new BloomParticle(glowPos, Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.75f, 1f), Color.Orange, 0.4f * strength, Main.rand.Next(30, 50), 1, EmberUpdate));

			Vector2 pos = Projectile.Center;
			Vector2 velocity = Main.rand.NextVector2CircularEdge(13f, 13f) * Main.rand.NextFloat(0.5f, 1f) * strength;

			ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.Orange, Color.Goldenrod, Main.rand.NextFloat()), 1.35f, Main.rand.Next(10, 30), p => p.Velocity *= 0.9f, tileCollide: false));
		
			for (int x = 0; x < 3; x++)
			{
				Dust.NewDustPerfect(pos, DustID.Torch, Main.rand.NextVector2Circular(9f, 9f) * strength, 0, default, Main.rand.NextFloat(3f) * strength).noGravity = true;
			}
		}

		ParticleHandler.SpawnParticle(new BloomParticle(glowPos, Vector2.Zero, Color.DarkOrange, 1.33f * strength, 40));
		
		ParticleHandler.SpawnParticle(new BloomParticle(glowPos, Vector2.Zero, Color.Orange, 1 * strength, 30));

		float rotation = Main.rand.NextFloat(6.28f);
		float scorchScale = Main.rand.NextFloat(0.15f, 0.3f) * strength;

		ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, Color.DarkOrange.Additive(), rotation, scorchScale, Main.rand.NextFloat(0.1f, 0.2f), "Fire1", new(0.5f, 0.5f), new(4, 0.5f), 35)
		{
			DistortEasing = EaseFunction.EaseQuadInOut,
			Intensity = 0.5f,
			Layer = ParticleLayer.BelowNPC,
			Pixellate = true,
			PixelDivisor = 2,
		});

		ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, Color.Orange.Additive(), rotation, scorchScale * 0.6f, Main.rand.NextFloat(0.1f, 0.2f), "Fire1", new(0.5f, 0.5f), new(4, 0.5f), 20)
		{
			DistortEasing = EaseFunction.EaseQuadInOut,
			Intensity = 0.5f,
			Layer = ParticleLayer.BelowNPC,
			Pixellate = true,
			PixelDivisor = 2,
		});

		static void EmberUpdate(Particle p)
		{
			p.Velocity *= 0.96f;
			p.Velocity.Y += 0.06f;

			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new SmokeCloud(p.Position + Main.rand.NextVector2Circular(5, 5), Main.rand.NextVector2Circular(0.5f, 0.5f), new Color(90, 90, 90) * 0.5f * (1 - p.Progress), 0.06f, EaseFunction.EaseQuadOut, 30)
				{	
					Pixellate = true,
					PixelDivisor = 3
				});

			Dust.NewDustPerfect(p.Position + Main.rand.NextVector2Circular(5, 5), DustID.Torch, null, 0, default, Main.rand.NextFloat(2f)).noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (Projectile.penetrate < 0)
			return false;

		var texture = TextureAssets.Projectile[Type].Value;

		Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0f, 0f);

		return false;
	}
}
