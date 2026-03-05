using Microsoft.Build.Construction;
using SpiritReforged.Common.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

public class AdornedFlash : ModProjectile
{
	public override string Texture => AssetLoader.EmptyTexture;

	public Vector2 originalCenter;

	public override void SetDefaults()
	{
		Projectile.Size = new(30);
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;

		Projectile.tileCollide = false;
		Projectile.penetrate = 1;

		Projectile.stopsDealingDamageAfterPenetrateHits = true;

		Projectile.timeLeft = 30;
	}

	public override void AI()
	{

	}

	public override bool PreDraw(ref Color lightColor)
	{
		var ray = AssetLoader.LoadedTextures["Godray"].Value;

		float dist = Projectile.Distance(originalCenter);

		float distProgress = dist / 300f; // 300 units is as far away as they can reach

		float rot = originalCenter.DirectionTo(Projectile.Center).ToRotation();

		float progress = Projectile.timeLeft / 30f;

		Main.EntitySpriteDraw(ray, originalCenter + rot.ToRotationVector2() * 165 * distProgress - Main.screenPosition, null, Color.White.Additive() * progress, rot - MathHelper.PiOver2, ray.Size() / 2f, new Vector2(0.05f, 0.02f + 0.3f * distProgress), 0f);

		return false;
	}
}
