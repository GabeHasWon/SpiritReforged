using Terraria.Audio;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Forest.FairyWhistle;

[AutoloadMinionBuff]
[AutoloadGlowmask("255,255,255", false)]
public class FairyMinion : BaseMinion
{
	private ref float AiTimer => ref Projectile.ai[0];
	private ref float Style => ref Projectile.ai[1];

	private const int SHOOTTIME = 50;

	private bool doAttackAnimation = false;

	public FairyMinion() : base(400, 500, new Vector2(20, 20)) { }

	public static Color StyleColor(float style) => (int)style switch
	{
		1 => new(72, 142, 255),
		2 => new(215, 79, 137),
		_ => new(0, 193, 141),
	};

	public override void AbstractSetStaticDefaults() => Main.projFrames[Type] = 6;
	public override void AbstractSetDefaults() => Projectile.alpha = 255;

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe)
	{
		if (doAttackAnimation)
		{
			framespersecond = 15;
			endframe = 6; //endFrame is visually 6

			if (Projectile.frame == 5)
			{
				Reset();
				doAttackAnimation = false;
			}
		}
		else
		{
			framespersecond = (int)MathHelper.Lerp(10, 20, Math.Min(Projectile.velocity.Length() / 6, 1));
			endframe = 5;

			if (AiTimer == SHOOTTIME - 20)
			{
				Reset();
				doAttackAnimation = true;
			}
		}

		return true;

		void Reset()
		{
			Projectile.frameCounter = 0;
			Projectile.frame = 0;
		}
	}

	public override bool MinionContactDamage() => false;

	public override bool PreAI()
	{
		Projectile.rotation = Projectile.velocity.X * 0.05f;
		Projectile.alpha = Math.Max(Projectile.alpha - 3, 0);

		foreach (Projectile p in Main.ActiveProjectiles)
		{
			if (p.whoAmI != Projectile.whoAmI && p.type == Type && p.owner == Projectile.owner && p.Hitbox.Intersects(Projectile.Hitbox))
				Projectile.velocity += Projectile.DirectionFrom(p.Center) / 10;
		}

		return true;
	}

	public override void IdleMovement(Player player)
	{
		AiTimer = 0;

		if (Math.Abs(Projectile.velocity.X) > 1) //dont flip too fast
			Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X) > 0 ? -1 : 1;

		Vector2 desiredPosition = player.MountedCenter - new Vector2(0, 48 + (float)Math.Sin(Main.GameUpdateCount / 6f) * 6);
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Lerp(Projectile.Center, desiredPosition, 0.15f) - Projectile.Center, 0.1f);

		if (Projectile.Distance(desiredPosition) > 600) //Teleport
			Projectile.Center = desiredPosition;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		Projectile.direction = Projectile.spriteDirection = Math.Sign(target.Center.X - Projectile.Center.X);

		var desiredPosition = player.MountedCenter - new Vector2(0, 60 + (float)Math.Sin(Main.GameUpdateCount / 6f) * 6) + Projectile.DirectionTo(target.Center) * 30;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Lerp(Projectile.Center, desiredPosition, 0.15f) - Projectile.Center, 0.1f);

		if (++AiTimer >= SHOOTTIME)
		{
			Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.DirectionTo(target.Center) * 3.5f, 
				ModContent.ProjectileType<FairyProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Style);

			Projectile.velocity -= Projectile.DirectionTo(target.Center) * 4;
			AiTimer = 0 + Main.rand.Next(-5, 6);
			Projectile.netUpdate = true;

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Item9 with { PitchVariance = 0.3f }, Projectile.Center);

				for (int i = 0; i < 10; i++)
					ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 1f),
						StyleColor(Style) * 0.66f, Main.rand.NextFloat(0.35f, 0.5f), 35, 10, p => RandomCurveParticleMovement(p, 0.12f, 0.97f)));
			}
		}
	}

	internal static void RandomCurveParticleMovement(Particle p, float maxRadians, float decelerationRate) => p.Velocity = p.Velocity.RotatedByRandom(maxRadians) * decelerationRate;

	public override bool PreDraw(ref Color lightColor)
	{
		float bloomOpacity = EaseFunction.EaseQuadOut.Ease(AiTimer / SHOOTTIME); //glow brighter when closer to shot time
		bloomOpacity = Math.Max(bloomOpacity, .25f);

		var texture = TextureAssets.Projectile[Type].Value;
		var glowmask = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;
		var source = texture.Frame(6, Main.projFrames[Type], (int)Style * 2 + (doAttackAnimation ? 1 : 0), Projectile.frame, -2, -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, default);
		Main.EntitySpriteDraw(glowmask, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(Color.White) * bloomOpacity, Projectile.rotation, source.Size() / 2, Projectile.scale, default);

		return false;
	}
}