using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Dusts;

namespace SpiritReforged.Content.Forest.Glyphs.Storm;

public class ZephyrWave : ModProjectile
{
	public override string Texture => "Terraria/Images/Projectile_985";

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(30);
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.alpha = 255;
		Projectile.scale = 0.75f;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];

		if (Projectile.frame < 3)
			Projectile.timeLeft = 2;
		if (Projectile.alpha > 200)
			Projectile.alpha = Math.Max(Projectile.alpha - 10, 200);

		if (++Projectile.frameCounter >= 2)
		{
			Projectile.frameCounter = 0;
			Projectile.frame = Math.Min(Projectile.frame + 1, 3);
		}

		if (Main.rand.NextBool(3))
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<Wind>());
			dust.customData = new Wind.WindAnchor(Projectile.Center, Projectile.velocity, dust.position);
		}

		Projectile.Center = owner.Center;
		float offset = (1 - (1f - (float)owner.itemAnimation / owner.itemAnimationMax) * 4f) * Projectile.direction;
		Projectile.rotation = Projectile.velocity.ToRotation() - offset;
		Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X < 0) ? -1 : 1;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle frame = texture.Frame(1, 4, 0, Projectile.frame, 0, 0);
		SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

		float opacity = 1;
		for (int i = 0; i < 4; i++)
		{
			float rotation = Projectile.rotation - i * 0.3f;
			Color color = Projectile.GetAlpha(Color.White.Additive()) * opacity;
			opacity -= 0.3f;

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
				color, rotation, frame.Size() / 2, Projectile.scale, effects, 0);
		}

		return false;
	}
}