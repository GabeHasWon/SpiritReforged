using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Granite.Armor;

public class EnergyImpact : EnergyPlunge, IDrawOverTiles
{
	public const int TimeLeftMax = 180;

	public override void SetDefaults()
	{
		base.SetDefaults();
		Projectile.timeLeft = TimeLeftMax;
	}

	public override void AI()
	{
	}

	public void DrawOverTiles(SpriteBatch spriteBatch)
	{
		var texture = ParticleHandler.GetTexture(ParticleHandler.TypeOf<Shatter>());
		var color = Color.Black * ((float)Projectile.timeLeft / TimeLeftMax) * 0.4f;

		spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color, 0, texture.Size() / 2, Projectile.scale, default, 0);
	}
}

public class EnergyPlunge : ModProjectile
{
	public override string Texture => AssetLoader.EmptyTexture;

	public static bool Stomping(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<EnergyPlunge>()] > 0;
	public static void Begin(Player player)
	{
		Projectile.NewProjectile(player.GetSource_FromThis("DoubleTap"), player.Center, Vector2.Zero, ModContent.ProjectileType<EnergyPlunge>(), 10, 10, player.whoAmI);
		player.velocity.Y -= 5;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(30);
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];
		Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter);
		Projectile.timeLeft = 2;

		if (owner.velocity.Y == 0)
		{
			Projectile.Kill();
			Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<EnergyImpact>(), 10, 10, Projectile.owner);
		}
	}

	public override bool ShouldUpdatePosition() => false;
}