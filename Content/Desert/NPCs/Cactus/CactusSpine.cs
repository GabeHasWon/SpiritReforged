using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

public class CactusSpine : ModProjectile
{
	private bool _spawned = true;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 20;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(6);
		Projectile.DamageType = DamageClass.Magic;
		Projectile.hostile = true;
		Projectile.penetrate = -1;
		Projectile.extraUpdates = 3;
		Projectile.timeLeft = 300;
		Projectile.scale = Main.rand.NextFloat(0.7f, 1.1f);
	}

	public override void AI()
	{
		if (_spawned) //Create a trail
		{
			if (!Main.dedServ)
				TrailSystem.ProjectileRenderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(new Color(87, 35, 88) * 0.2f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 8 * Projectile.scale, 75));

			_spawned = false;
		}

		Projectile.velocity.Y += 0.02f;
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
	}

	public void PostHitPlayer(Player target)
	{
		Projectile.tileCollide = false;
		Projectile.alpha = 0;

		if (!Main.dedServ)
		{
			TrailSystem.ProjectileRenderer.DissolveTrail(Projectile, 12);
			SoundEngine.PlaySound(Ocean.Items.Reefhunter.Projectiles.UrchinSpike.Impact, Projectile.Center);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDraw();
		Projectile.QuickDrawTrail(baseOpacity: 0.25f);

		return false;
	}
}