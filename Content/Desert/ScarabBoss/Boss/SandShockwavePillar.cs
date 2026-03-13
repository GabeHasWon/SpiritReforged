using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class SandShockwavePillar : ModProjectile
{
	private const int MAX_TIMELEFT = 20;

	private ref float SpawnDelay => ref Projectile.ai[0];
	private ref float AITimer => ref Projectile.ai[2];
	private int HitboxHeight => (int)Projectile.ai[1];

	private float ParticleVerticalSpeedMult => HitboxHeight / 120f;

	private float Progress => EaseFunction.EaseCircularOut.Ease((AITimer - SpawnDelay) / MAX_TIMELEFT);

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults() => base.SetStaticDefaults();

	public override void SetDefaults()
	{
		Projectile.Size = new(40, 40);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.hide = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanDamage() => AITimer > SpawnDelay ? null : false;

	public override void AI()
	{
		if (AITimer == 0)
			Projectile.timeLeft += (int)SpawnDelay;

		if (AITimer++ == SpawnDelay)
			OnSpawn();

		if (Main.dedServ)
			return;

		if (AITimer > SpawnDelay)
		{
			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom, -Vector2.UnitY * Main.rand.NextFloat(2, 6) * ParticleVerticalSpeedMult, new Color(253, 239, 167) * 0.7f, Main.rand.NextFloat(0.05f, 0.25f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
			{
				Pixellate = true,
				DissolveAmount = 1,
				SecondaryColor = new Color(148, 138, 90) * 0.7f,
				TertiaryColor = new Color(118, 116, 66) * 0.7f,
				PixelDivisor = 3,
				ColorLerpExponent = 0.25f,
				Layer = ParticleLayer.BelowSolid
			});

			if (Main.rand.NextBool(4))
			{
				Vector2 dustPosition = Projectile.Center + Vector2.UnitY * 4f;
				Point tilePosition = dustPosition.ToTileCoordinates();
				int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

				Dust dust = Main.dust[dustIndex];
				dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-16f, 16f);
				dust.velocity.Y -= Main.rand.NextFloat(1.5f, 3f) * ParticleVerticalSpeedMult;
				dust.velocity.X *= 0.5f;
				dust.noLightEmittence = true;
				dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
			}
		}

		//Telegraph
		else if (false)
		{
			for (int i = 0; i < Main.rand.Next(3); i++)
			{
				Vector2 particlePos = Projectile.Bottom;
				particlePos += Main.rand.NextFloat(-16, 16) * Vector2.UnitX;

				Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 10);

				ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, Color.Beige, Main.rand.NextFloat(0.04f, 0.1f), EaseFunction.EaseCircularOut, Main.rand.Next(20, 30))
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.25f,
					Layer = ParticleLayer.BelowSolid
				});
			}

			if (Main.rand.NextBool(3))
				Dust.NewDust(Projectile.BottomLeft - Vector2.UnitY * 8, Projectile.width, 0, DustID.Sand, 0, -4, 0, default, Main.rand.NextFloat(0.5f, 0.9f));

			if (AITimer == SpawnDelay - 15 || AITimer == (SpawnDelay / 2) - 15)
			{
				int numTiles = 3;
				float maxHeight = 6;
				int totalTime = 30;

				for (int j = -1; j <= 1; j += 2)
				{
					for (float i = 0; i < numTiles; i++)
					{
						float height = MathHelper.Lerp(maxHeight, 0, i / numTiles);
						int delay = (int)MathHelper.Lerp(0, totalTime / 2, (i + 1) / numTiles);
						ParticleHandler.SpawnQueuedParticle(new MovingBlockParticle(Projectile.Bottom + j * Vector2.UnitX * 16 * (i + 1), totalTime / 2, height), delay);
					}
				}

				ParticleHandler.SpawnParticle(new MovingBlockParticle(Projectile.Bottom, totalTime / 2, maxHeight));
			}
		}
	}

	private void OnSpawn()
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		Vector2 dustPosition = Projectile.Center + Vector2.UnitY * 4f;
		Point tilePosition = dustPosition.ToTileCoordinates();
		float ySpeedMult = ParticleVerticalSpeedMult;

		int dustCount = HitboxHeight / 15;
		Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, HitboxHeight / 10f, 5, 35, 600, "shockwaveSand"));

		for (int i = 0; i < dustCount; i++)
		{
			int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

			Dust dust = Main.dust[dustIndex];
			dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-16f, 16f);
			dust.velocity.Y = -Main.rand.NextFloat(1.5f, 8f) * ySpeedMult;
			dust.velocity.X *= 0.3f;
			dust.noLightEmittence = true;
			dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
			dust.velocity += Projectile.velocity;
			dust.noGravity = false;
		}

		int smokeCount = Math.Clamp(HitboxHeight / 40, 1, 6);
		for (int i = 0; i < smokeCount; i++)
		{
			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom + Main.rand.NextFloat(-64, 64) * Vector2.UnitX, -Vector2.UnitY * Main.rand.NextFloat(1, 11) * ySpeedMult + Projectile.velocity * 0.4f, new Color(243, 239, 187), Main.rand.NextFloat(0.1f, 0.2f), EaseFunction.EaseCircularOut, Main.rand.Next(50, 90))
			{
				Pixellate = true,
				DissolveAmount = 1,
				SecondaryColor = new Color(148, 138, 90),
				TertiaryColor = new Color(118, 116, 66),
				PixelDivisor = 3,
				ColorLerpExponent = 0.25f,
				Layer = ParticleLayer.BelowSolid
			});
		}

		int chunkCount =  Math.Clamp(HitboxHeight / 40, 1, 6);
		for (int i = 0; i < chunkCount; i++)
		{
			float verticalVelocity = Main.rand.NextFloat(3f, 8f) * ySpeedMult;
			ParticleHandler.SpawnParticle(new TileChunkParticle(tilePosition, Projectile.Bottom + Main.rand.NextFloat(-14, 14) * Vector2.UnitX, -Vector2.UnitY * verticalVelocity + Projectile.velocity * Main.rand.NextFloat(1f, 3f), Main.rand.Next(40, 80), Main.rand.NextBool(4)));
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		Rectangle recenteredHitbox = new Rectangle((int)Projectile.Center.X - 10, (int)Projectile.position.Y - HitboxHeight, 20, HitboxHeight);
		return recenteredHitbox.Intersects(targetHitbox);
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		modifiers.HitDirectionOverride = Math.Sign(Projectile.velocity.X);
	}

	public override bool PreDraw(ref Color lightColor) => false;
}