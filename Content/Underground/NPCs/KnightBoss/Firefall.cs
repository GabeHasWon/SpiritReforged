using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Underground.NPCs.KnightBoss;

public class Firefall : ModProjectile
{
	public const float Gravity = 0.25f;

	public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

	public override void SetDefaults()
	{
		Projectile.Size = new(12);
		Projectile.hostile = true;
	}

	public override void AI()
	{
		Projectile.UpdateFrame(20);

		Projectile.velocity.Y += Gravity;
		Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

		if (Main.rand.NextBool(3))
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Scale: 1.2f).noGravity = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		ProjectileID.Sets.TrailingMode[Type] = 2;
		ProjectileID.Sets.TrailCacheLength[Type] = 4;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

		Projectile.QuickDrawTrail(drawColor: Color.White.Additive());

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, Projectile.GetAlpha(Color.White.Additive()), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);

		return false;
	}
}