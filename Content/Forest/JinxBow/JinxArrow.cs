using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxArrow : ModProjectile
{
	private const int MAX_TIMELEFT = 40;

	private ref float StuckNPC => ref Projectile.ai[0];
	private bool HasStruckNPC { get => Projectile.ai[1] == 1; set => Projectile.ai[1] = value ? 1 : 0; }

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionShot[Projectile.type] = true;
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(2);
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.tileCollide = false;
		Projectile.usesIDStaticNPCImmunity = true;
		Projectile.idStaticNPCHitCooldown = -1;
	}

	public override bool PreAI()
	{
		return true;
	}

	public override void AI()
	{
		const float offsetMagnitude = 5;

		NPC stuckNPC = Main.npc[(int)StuckNPC];
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		if(!stuckNPC.active)
		{
			Projectile.Kill();
			return;
		}

		float timeLeftProgress = EaseFunction.EaseCubicIn.Ease(EaseFunction.EaseCircularIn.Ease(Projectile.timeLeft / (float)MAX_TIMELEFT));
		Projectile.position = stuckNPC.Center + Vector2.Lerp(-Projectile.velocity, Projectile.velocity / 4, 1 - timeLeftProgress) * offsetMagnitude;
		Lighting.AddLight(Projectile.Center, Color.MediumPurple.ToVector3() / 3);
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.SetCrit();
		HasStruckNPC = true;
	}

	public override bool? CanHitNPC(NPC target) => !HasStruckNPC && target.whoAmI == (int)StuckNPC;

	public override bool? CanDamage() => !HasStruckNPC;

	public override bool ShouldUpdatePosition() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Projectile.type].Value;

		Color drawColor = Color.MediumPurple.Additive(50) * EaseFunction.EaseCircularOut.Ease(Projectile.timeLeft / (float)MAX_TIMELEFT);
		Vector2 drawPos = Projectile.Center - Main.screenPosition;

		Projectile.QuickDrawTrail(baseOpacity: 0.25f, drawColor: drawColor);
		for(int i = 0; i < 12; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f);

			Main.EntitySpriteDraw(projTex, drawPos + offset, null, drawColor * 0.2f, Projectile.rotation, projTex.Size() / 2, Projectile.scale, SpriteEffects.None);
		}

		Projectile.QuickDraw(drawColor: Color.MediumPurple.Additive(50) * EaseFunction.EaseCircularIn.Ease(Projectile.timeLeft / (float)MAX_TIMELEFT));
		return false;
	}
}