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
		Projectile.timeLeft = MAX_TIMELEFT + (int)SpawnDelay;
	}

	public override void AI()
	{
		if (AITimer++ == SpawnDelay)
			OnSpawn();

		if(AITimer > SpawnDelay)
		{
			if(Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom, -Vector2.UnitY * Main.rand.NextFloat(16), Color.LightGoldenrodYellow, Main.rand.NextFloat(0.05f, 0.2f), EaseFunction.EaseCircularOut, 30)
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.5f,
					Layer = ParticleLayer.BelowSolid
				});
			}
		}
	}

	private void OnSpawn()
	{
		if (Main.netMode != NetmodeID.Server)
		{
			for (int i = 0; i < 6; i++)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom, -Vector2.UnitY * Main.rand.NextFloat(6), Color.LightGoldenrodYellow, Main.rand.NextFloat(0.05f, 0.2f), EaseFunction.EaseCubicOut, 30)
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.5f,
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