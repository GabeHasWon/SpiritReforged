using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Underground.NPCs;

public class WheezerCloud : ModProjectile
{
	public const int Size = 40;
	public const int TimeLeftMax = 280;

	public float Progress => Projectile.timeLeft / (float)TimeLeftMax;
	public bool DealDamage => Projectile.velocity.Length() > .5f;

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 8;

		MoRHelper.AddElement(Projectile, MoRHelper.Wind);
	}
	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(Size);
		Projectile.timeLeft = TimeLeftMax;
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.Opacity = 0;
	}

	public override void AI()
	{
		Projectile.Opacity = Progress;
		Projectile.scale += 1f / TimeLeftMax;
		Projectile.rotation += Projectile.velocity.X * .01f;

		Projectile.velocity *= .9f;
		Projectile.velocity.Y = Math.Max(Projectile.velocity.Y - 0.02f, -0.5f); //Slowly rise over time
		Projectile.UpdateFrame(15);

		int square = (int)(Size * Projectile.scale);
		Projectile.Resize(square, square);
		var area = Projectile.getRect();

		foreach (var p in Main.ActivePlayers)
		{
			if (area.Contains(p.Center.ToPoint()))
				p.AddBuff(BuffID.Poisoned, 600);
		}
	}

	public override bool CanHitPlayer(Player target) => DealDamage;
	public override bool? CanHitNPC(NPC target) => DealDamage ? null : false;

	public override bool PreDraw(ref Color lightColor)
	{
		const int images = 3;

		var texture = TextureAssets.Projectile[Type].Value;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
		var color = Projectile.GetAlpha(lightColor).Additive(170) * .75f;

		for (int i = 0; i < images; i++)
		{
			var position = Projectile.Center - Main.screenPosition + (Vector2.UnitX * (20 * (1f - Progress)) * Projectile.scale).RotatedBy(MathHelper.TwoPi / images * i);
			Main.EntitySpriteDraw(texture, position, source, color, Projectile.rotation + i, source.Size() / 2, Projectile.scale, default);
		}

		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		=> overPlayers.Add(index);
}