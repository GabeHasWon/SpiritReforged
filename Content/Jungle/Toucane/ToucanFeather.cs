using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Jungle.Toucane;

public class ToucanFeather : ModProjectile
{
	private const int maxtimeleft = 360;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionShot[Type] = true;
		ProjectileID.Sets.TrailCacheLength[Type] = 10;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(10, 10);
		Projectile.scale = Main.rand.NextFloat(0.5f, 0.6f);
		Projectile.friendly = true;
		Projectile.penetrate = 1;
		Projectile.timeLeft = maxtimeleft;
		Projectile.extraUpdates = 1;
		Projectile.alpha = 255;
	}

	public override void AI()
	{
		Projectile.alpha = Math.Max(Projectile.alpha - 20, 0);
		if (Projectile.timeLeft > maxtimeleft - 10)
			Projectile.scale *= 1.08f;

		if (Projectile.velocity.Length() < 12)
			Projectile.velocity *= 1.03f;

		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		if (!Projectile.wet)
			Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() / 2);
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.netMode != NetmodeID.Server)
			SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

		for (int i = 0; i < 5; i++)
		{
			var dust = Dust.NewDustPerfect(Projectile.Center, 90, Projectile.velocity.RotatedByRandom(MathHelper.Pi / 12) * Main.rand.NextFloat(0.5f, 0.7f), 100, default, Main.rand.NextFloat(0.7f, 1f));
			dust.fadeIn = 0.75f;
			dust.noGravity = true;
		}

		for (int j = 0; j < 10; j++)
		{
			var dust = Dust.NewDustPerfect(Projectile.Center, 90, Projectile.velocity.RotatedByRandom(MathHelper.Pi / 3) * Main.rand.NextFloat(0.1f, 0.3f), 100, default, Main.rand.NextFloat(0.2f, 0.4f));
			dust.fadeIn = 0.75f;
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDrawTrail(Main.spriteBatch);
		Projectile.QuickDraw(Main.spriteBatch);

		if (!Projectile.wet)
		{
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			var color = Color.Lerp(new Color(255, 0, 89, 0), new Color(255, 47, 0, 0), (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3) / 2 + 0.5f);
			Vector2 stretch = new Vector2(0.5f, 1f) / 5;
			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, color * 0.5f, Projectile.rotation, bloom.Size() / 2, Projectile.scale * 1.5f * stretch, SpriteEffects.None, 0);

			//color *= 0.7f * Projectile.Opacity;

			//Projectile.QuickDrawGlowTrail(spriteBatch, 0.9f, color);
			//Projectile.QuickDrawGlow(spriteBatch, color);
		}

		return false;
	}
}