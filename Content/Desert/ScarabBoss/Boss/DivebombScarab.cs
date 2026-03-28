using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class DivebombScarab : ModProjectile
{
	private const int BACKGROUND_TIME = 90;

	private float _bgDistance = 1;

	private Vector2 _spawnOrigin;

	private ref float AITimer => ref Projectile.ai[0];

	public override void SetStaticDefaults()
	{
		Main.projFrames[Projectile.type] = 4;
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(48, 48);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.hide = true;
		Projectile.penetrate = -1;
		Projectile.alpha = 0;
	}

	public override void AI()
	{
		if (AITimer++ == 0)
			OnSpawn();

		//go up, enter foreground from bg, then slam downwards and tile collide

		if (Projectile.frameCounter++ > 2)
		{
			if (Projectile.frame >= 3)
				Projectile.frame = 0;

			else
				Projectile.frame++;

			Projectile.frameCounter = 0;
		}

		if (AITimer <= BACKGROUND_TIME)
			BGFlying();
		else
			DiveBomb();
	}

	private void OnSpawn()
	{
		Projectile.netUpdate = true;
		_spawnOrigin = Main.screenPosition;
	}

	private void BGFlying()
	{
		float progress = AITimer / BACKGROUND_TIME;
		Projectile.velocity.Y = MathHelper.Lerp(-16, 0, EaseFunction.EaseCircularOut.Ease(progress));
		_bgDistance = MathHelper.Lerp(0.75f, 0, EaseFunction.EaseQuadOut.Ease(progress));
	}

	private void DiveBomb()
	{
		const int ANTICIPATION_TIME = 30;

		if(AITimer < BACKGROUND_TIME + ANTICIPATION_TIME)
		{
			float progress = (AITimer - BACKGROUND_TIME) / ANTICIPATION_TIME;
			Projectile.velocity.Y = MathHelper.Lerp(-0.2f, -5, EaseFunction.EaseQuadIn.Ease(progress));
			Projectile.velocity.X = MathHelper.Lerp(0, 0.5f, EaseFunction.EaseCubicOut.Ease(progress));
			Projectile.frameCounter++;
		}

		if(AITimer == BACKGROUND_TIME + ANTICIPATION_TIME)
		{
			Projectile.velocity.Y = 20;
			Projectile.velocity.X = -4;
			Projectile.tileCollide = true;
		}
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

	public override void OnKill(int timeLeft)
	{
		Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
		for (int i = 0; i < 10; i++)
		{
			int d = Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Plantera_Green, Projectile.oldVelocity.X * 0.2f, Projectile.oldVelocity.Y * 0.2f);
			Main.dust[d].noGravity = true;
			Main.dust[d].scale = 1.2f;
		}

		SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);

		if (Main.netMode != NetmodeID.Server)
			for (int i = 1; i <= 3; i++)
				Main.gore[Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Projectile.velocity, Mod.Find<ModGore>("largescarab" + i.ToString()).Type)].timeLeft = 10;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
		float scale = Projectile.scale * (1 - _bgDistance);
		Vector2 position = Projectile.Center - Vector2.Lerp(Main.screenPosition, _spawnOrigin, (float)Math.Pow(_bgDistance, 1.5));
		var color = Color.Lerp(lightColor, Color.Black, _bgDistance / 3);

		if (Projectile.tileCollide == true)
			Projectile.QuickDrawTrail(baseOpacity: 0.2f);

		Main.EntitySpriteDraw(tex, position, Projectile.DrawFrame(), color, Projectile.rotation, Projectile.DrawFrame().Size() / 2, scale, SpriteEffects.None);

		return false;
	}
}