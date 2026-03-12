using SpiritReforged.Common.Easing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;
public class SunriseScepterShot : ModProjectile
{
	public float ParentID => Projectile.ai[0];
	internal SunriseScepterHeld Parent => Main.projectile[(int)ParentID].ModProjectile as SunriseScepterHeld;
	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(6);

		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;

		Projectile.tileCollide = true;
		Projectile.extraUpdates = 2;

		Projectile.timeLeft = 30;

		Projectile.penetrate = 1;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == 30)
		{
			Projectile.rotation = Parent.OrbPosition.DirectionTo(Projectile.Center).ToRotation();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var shineAlpha = AssetLoader.LoadedTextures["ShineAlpha"].Value;

		float fade = Projectile.timeLeft / 30f;

		Main.EntitySpriteDraw(shineAlpha, Parent.OrbPosition - Main.screenPosition, null, new Color(255, 255, 255, 0) * fade,
						Projectile.rotation + MathHelper.PiOver2, new Vector2(shineAlpha.Width / 2, shineAlpha.Height),
						new Vector2(0.5f, 0.4f), SpriteEffects.None);

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{

	}

	public override void OnKill(int timeLeft)
	{

	}

	/*public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{

	}*/
}
