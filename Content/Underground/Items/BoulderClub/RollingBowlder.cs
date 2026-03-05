using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.BoulderClub;

class RollingBowlder : ModProjectile
{
	public const int MaxPenetrate = 3;

	private int Direction { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }

	private bool HasCollided { get => Projectile.ai[1] == 1; set => Projectile.ai[1] = value ? 1 : 0; }

	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/StoneCrack2")
	{
		PitchRange = (0.2f, 0.8f),
		Volume = 0.3f
	};

	public static readonly SoundStyle Hit = SoundID.Item70 with
	{
		MaxInstances = 3
	};

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.DamageType = DamageClass.Melee;
		Projectile.Size = new Vector2(32);
		Projectile.friendly = true;
		Projectile.penetrate = MaxPenetrate;
		Projectile.timeLeft = 200;

		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void AI()
	{
		Projectile.velocity.Y += 0.5f;
		Projectile.rotation += Projectile.velocity.X * 0.06f;

		Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
	}

	public override void OnKill(int timeLeft)
	{
		var velocity = Projectile.oldVelocity;

		for (int i = 1; i < 6; i++)
		{
			int type = Mod.Find<ModGore>("Bowlder" + i).Type;
			Gore.NewGore(Projectile.GetSource_Death(), Projectile.position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), velocity * 0.5f, type, Projectile.scale);
		}

		for (int i = 0; i < 15; i++)
		{
			var random = (velocity * Main.rand.NextFloat()).RotatedByRandom(1);
			Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone, random.X, random.Y);
		}

		ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, velocity * 0.3f, Color.DarkGray, Projectile.scale * 0.15f, EaseFunction.EaseCircularOut, 40));

		SoundEngine.PlaySound(Hit, Projectile.Center);
		SoundEngine.PlaySound(Break, Projectile.Center);
	}

	//Reduce damage with hits
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.FinalDamage *= Math.Max(0.2f, 1f - Projectile.numHits / (float)MaxPenetrate);

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.velocity.X == 0)
			Projectile.Kill();

		if (oldVelocity.Y > 3.5f || !HasCollided)
		{
			SoundEngine.PlaySound(Hit.WithVolumeScale(0.5f), Projectile.Center);

			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, oldVelocity * 0.3f, Color.Gray, Projectile.scale * 0.15f, EaseFunction.EaseCircularOut, 40));
			Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

			Projectile.velocity.Y = oldVelocity.Y * -0.5f;

			const float minSpeed = BowlderProj.SHOOT_SPEED / 2;
			if (!HasCollided && Projectile.velocity.X is > -minSpeed and < minSpeed)
				Projectile.velocity.X = MathHelper.Clamp(Direction * -Projectile.velocity.Y, -minSpeed, minSpeed);

			HasCollided = true;
		}

		return false;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = false;
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float velocityRatio = Math.Abs(Projectile.velocity.Length() / BowlderProj.SHOOT_SPEED);
		velocityRatio = MathHelper.Clamp(velocityRatio, 0, 1);
		velocityRatio = EaseFunction.EaseQuadIn.Ease(velocityRatio);

		Projectile.QuickDrawTrail(baseOpacity: velocityRatio * 0.5f);
		Projectile.QuickDraw();
		
		return false;
	}
}