using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract class RapierProjectile : SwungProjectile
{
	public sealed class ParryPlayer : ModPlayer
	{
		public enum ParryState { Active, Inactive, Successful }

		public ParryState parryState;
		private int _resetTime;

		public override bool FreeDodge(Player.HurtInfo info)
		{
			if (parryState == ParryState.Active)
			{
				parryState = ParryState.Successful; //Needs synced
				_resetTime = 2;

				Player.SetImmuneTimeForAllTypes(30);

				return true;
			}

			return false;
		}

		public override void PostUpdate()
		{
			if (Math.Max(_resetTime -= 1, 0) > 0)
				return;

			parryState = ParryState.Inactive;
		}
	}

	public readonly record struct RapierConfiguration(EaseFunction Easing, int Reach, int Width, Func<Player.CompositeArmStretchAmount> Stretch, int SweetSpotScale) : IConfiguration;

	public ParryPlayer Global => Main.player[Projectile.owner].GetModPlayer<ParryPlayer>();
	public bool SecondaryUse { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

	protected bool hitSweetSpot;

	public override void AI()
	{
		base.AI();

		if (Main.player[Projectile.owner].GetModPlayer<ParryPlayer>().parryState == ParryPlayer.ParryState.Successful)
			OnParry(default);
	}

	public virtual void OnParry(Player.HurtInfo info) { }

	public override float GetRotation(out float armRotation)
	{
		int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
		float value = base.GetRotation(out armRotation) + direction * Progress * 2;

		return value + MathHelper.PiOver4;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (InSweetSpot(target, ((RapierConfiguration)Config).SweetSpotScale))
		{
			modifiers.SetCrit(); //Sweet spot crit
			hitSweetSpot = true;
		}
		else
		{
			modifiers.DisableCrit();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float offset = Math.Max(30 * (0.5f - Progress * 2), -2);
		DrawHeld(lightColor, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

		return false;
	}

	public override bool? CanDamage() => (!SecondaryUse && Counter <= 5) ? null : false;

	#region helpers
	public bool InSweetSpot(NPC target, int scale)
	{
		float reach = ((RapierConfiguration)Config).Reach - scale;
		return Projectile.Center.DistanceSQ(target.Hitbox.ClosestPointInRect(Projectile.Center)) > reach * reach;
	}

	public void DrawStar(Color lightColor, float scale, float intensity)
	{
		Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
		Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
		Vector2 position = GetEndPosition() - Main.screenPosition;

		Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.SteelBlue).Additive() * intensity, 0, star.Size() / 2, Projectile.scale * scale * intensity, default);
		Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.White).Additive() * intensity, 0, star.Size() / 2, Projectile.scale * 0.8f * scale * intensity, default);
	}
	#endregion
}