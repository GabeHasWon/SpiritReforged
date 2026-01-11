using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

public class SandPillar : ModProjectile
{
	private const int MAX_TIMELEFT = 40;

	private ref float SpawnDelay => ref Projectile.ai[0];
	private ref float AITimer => ref Projectile.ai[1];

	private float Progress => EaseFunction.EaseCircularOut.Ease((AITimer - SpawnDelay) / MAX_TIMELEFT);

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults() => base.SetStaticDefaults();

	public override void SetDefaults()
	{
		Projectile.Size = new(58, 160);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.hide = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;
	}

	public override void AI()
	{
		if(AITimer == 0)
			Projectile.timeLeft += (int)SpawnDelay;

		if (AITimer++ == SpawnDelay)
			OnSpawn();

		if(AITimer > SpawnDelay)
		{
			if(Main.netMode != NetmodeID.Server)
			{
				for(int i = 0; i < 1; i++)
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom, -Vector2.UnitY * Main.rand.NextFloat(8, 20), Color.LightGoldenrodYellow, Main.rand.NextFloat(0.05f, 0.25f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
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

				if(Main.rand.NextBool())
					Dust.NewDust(Projectile.BottomLeft, Projectile.width, 16, DustID.Sand, 0, Main.rand.NextFloat(-4, -8), 0, default, Main.rand.NextFloat(0.5f, 0.9f));
			}
		}
	}

	private void OnSpawn()
	{
		if (Main.netMode != NetmodeID.Server)
		{
			for (int i = 0; i < 6; i++)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom + Main.rand.NextFloat(-64, 64) * Vector2.UnitX, -Vector2.UnitY * Main.rand.NextFloat(2, 6), Color.Beige, Main.rand.NextFloat(0.1f, 0.2f), EaseFunction.EaseCircularOut, Main.rand.Next(50, 90))
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
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		Rectangle adjustedHitbox = projHitbox;
		adjustedHitbox.Y += adjustedHitbox.Height;
		adjustedHitbox.Height = (int)(projHitbox.Height * Progress);
		adjustedHitbox.Y -= adjustedHitbox.Height;

		return adjustedHitbox.Intersects(targetHitbox);
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

	public override bool PreDraw(ref Color lightColor)
	{
		if (AITimer < SpawnDelay)
			return false;

		/*
		int drawTimer = (int)(AITimer - SpawnDelay);

		Effect shockwaveEffect = AssetLoader.LoadedShaders["GroundShockwave"].Value;

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Length = Projectile.width,
			Height = Projectile.height,
			Position = Projectile.Center - Vector2.UnitY * Projectile.height / 2
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, shockwaveEffect);*/

		return false;
	}
}