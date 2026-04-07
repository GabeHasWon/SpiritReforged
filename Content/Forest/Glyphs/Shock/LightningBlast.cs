namespace SpiritReforged.Content.Forest.Glyphs.Shock;

public class LightningBlast : ModProjectile
{
	private const int FrameDuration = 4;

	public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

	public override void SetDefaults()
	{
		Projectile.Size = new(24);
		Projectile.timeLeft = FrameDuration * Main.projFrames[Type];
	}

	public override void AI()
	{
		if (++Projectile.frameCounter >= FrameDuration)
		{
			Projectile.frameCounter = 0;
			Projectile.frame++;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(Color.White), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);

		return false;
	}
}