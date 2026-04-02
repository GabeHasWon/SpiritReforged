using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Dusts;

namespace SpiritReforged.Content.Forest.Glyphs.Storm;

public class SlicingGust : ModProjectile
{
	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = -1;
		Projectile.tileCollide = true;
		Projectile.timeLeft = 60;
		Projectile.Size = new Vector2(14);
		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		if (Projectile.timeLeft < 20)
			Projectile.alpha += 255 / 20;

		Projectile.rotation = Projectile.velocity.ToRotation() + 1.57f;

		if (Main.rand.NextBool(3))
		{
			var dust = Dust.NewDustDirect(Projectile.position - new Vector2(4, 4), Projectile.width + 8, Projectile.height + 8, ModContent.DustType<Wind>());
			dust.velocity = Projectile.velocity * 0.2f;
			dust.customData = new Wind.WindAnchor(Projectile.Center, Projectile.velocity, dust.position);
		}

		if (Main.rand.NextBool(12))
		{
			int type = Main.rand.Next(61, 64);
			var gore = Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), Vector2.Zero, type, Main.rand.NextFloat(0.3f, 0.75f));
			gore.alpha = 150;
			gore.velocity = (Projectile.velocity * Main.rand.NextFloat(0.1f, 0.3f)).RotatedByRandom(0.2f);
			gore.position -= new Vector2(gore.Width, gore.Height) * 0.5f * gore.scale;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.velocity.Y -= hit.Knockback * target.knockBackResist;

	public override void OnKill(int timeLeft)
	{
		if (timeLeft <= 0)
			return;

		for (int i = 0; i < 6; i++)
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<Wind>());
			dust.customData = new Wind.WindAnchor(Projectile.Center, Projectile.velocity, dust.position);
		}

		for (int i = 0; i < 3; i++)
		{
			int type = Main.rand.Next(61, 64);
			var gore = Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), Vector2.Zero, type, Main.rand.NextFloat(0.5f, 1f));
			gore.alpha = Main.rand.Next(90, 130);
			gore.velocity = (Projectile.velocity * -Main.rand.NextFloat(0f, 0.15f)).RotatedByRandom(1f);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDrawTrail(drawColor: Color.White.Additive(), rotation: Projectile.rotation);
		return false;
	}
}