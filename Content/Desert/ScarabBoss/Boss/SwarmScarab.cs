using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class SwarmScarab : ModProjectile
{
	private const int FOREGROUND_TIME = 60;

	private float _fgDistance = 1;

	private Vector2 _spawnOrigin;

	private ref float AITimer => ref Projectile.ai[0];

	private ref float Direction => ref Projectile.ai[1];

	public override void SetStaticDefaults()
	{
		Main.projFrames[Projectile.type] = 2;
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(48, 48);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.hide = true;
		Main.projFrames[Projectile.type] = 2;
		Projectile.penetrate = -1;
		Projectile.alpha = 0;
	}

	public override void AI()
	{
		Projectile.direction = Projectile.spriteDirection = (int)Direction;
		if (AITimer++ == 0)
			OnSpawn();

		//go up, enter screen from foreground, then do a dashing sweep

		if (Projectile.frameCounter++ > 2)
		{
			if (Projectile.frame >= 1)
				Projectile.frame = 0;

			else
				Projectile.frame++;

			Projectile.frameCounter = 0;
		}

		if (AITimer <= FOREGROUND_TIME)
			FGFlying();
		else
			DashSweep();
	}

	private void OnSpawn()
	{
		Projectile.netUpdate = true;
		_spawnOrigin = Main.screenPosition;
	}

	private void FGFlying()
	{
		float progress = AITimer / FOREGROUND_TIME;
		Projectile.velocity = new Vector2(MathHelper.Lerp(-9, -3, EaseFunction.EaseCircularIn.Ease(progress)) * Direction,
			MathHelper.Lerp(4, 0, EaseFunction.EaseQuadIn.Ease(progress)));

		_fgDistance = MathHelper.Lerp(1, 0, EaseFunction.EaseQuadInOut.Ease(progress));
	}

	private void DashSweep()
	{
		const int anticipation_time = 40;
		const int dash_time = 60;
		const int dash_rest_time = 40;

		int DashStartTime = FOREGROUND_TIME + anticipation_time;
		int DashEndTime = DashStartTime + dash_time;
		int FlyOffTime = DashEndTime + dash_rest_time;

		//Anticipation before dash, moving backwards and speeding up animation
		if (AITimer < DashStartTime)
		{
			float progress = (AITimer - FOREGROUND_TIME) / anticipation_time;
			Projectile.velocity = new Vector2(MathHelper.Lerp(-3, 7, EaseFunction.EaseQuadIn.Ease(progress)) * Direction,
				MathHelper.Lerp(0, 0.5f, EaseFunction.EaseCubicOut.Ease(progress)));

			Projectile.frameCounter++;
		}

		//Set dash speed
		if (AITimer == DashStartTime)
			Projectile.velocity = new Vector2(-18 * Direction, 3);

		//Gradually curve back upwards
		if(AITimer > DashStartTime && AITimer < DashEndTime)
			Projectile.velocity.Y -= 0.1f;

		//Slow down and reduce animation speed
		if(AITimer > DashEndTime && AITimer < FlyOffTime)
		{
			Projectile.velocity *= 0.95f;

			if (AITimer % 2 == 0)
				Projectile.frameCounter--;
		}

		//Fly upwards and fade out
		if(AITimer > FlyOffTime)
		{
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, new Vector2(-1 * Direction, -4), 0.04f);
			if (AITimer % 2 == 0)
				Projectile.frameCounter++;

			Projectile.alpha += 12;
			if (Projectile.alpha >= 255)
				Projectile.Kill();
		}
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
		float scale = Projectile.scale * (1 + _fgDistance * 1.5f);
		Vector2 position = Projectile.Center - Vector2.Lerp(Main.screenPosition, Main.screenPosition - 2 * (_spawnOrigin - Main.screenPosition), (float)Math.Pow(_fgDistance, 1.5f) / 2);
		var color = Color.Lerp(lightColor, Color.Black, _fgDistance);
		color *= 1 - EaseFunction.EaseCircularIn.Ease(EaseFunction.EaseCircularIn.Ease(_fgDistance));

		//Dash check based on x velocity
		if (Math.Abs(Projectile.velocity.X) > 10)
			Projectile.QuickDrawTrail(baseOpacity: 0.2f);

		Main.EntitySpriteDraw(tex, position, Projectile.DrawFrame(), color * Projectile.Opacity, Projectile.rotation, Projectile.DrawFrame().Size() / 2, scale, Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

		return false;
	}
}