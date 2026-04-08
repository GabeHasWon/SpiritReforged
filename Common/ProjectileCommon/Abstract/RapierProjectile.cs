using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract class RapierProjectile : SwungProjectile
{
	public sealed class ParryPlayer : ModPlayer
	{
		public enum ParryState { Active, Inactive, Successful }

		public ParryState parryState;
		private bool _wasSuccessful;

		public override bool FreeDodge(Player.HurtInfo info)
		{
			if (parryState == ParryState.Active)
			{
				parryState = ParryState.Successful; //Needs synced
				_wasSuccessful = true;

				Player.SetImmuneTimeForAllTypes(30);

				return true;
			}

			return false;
		}

		public override void PostUpdate()
		{
			if (_wasSuccessful)
			{
				_wasSuccessful = false;
				return;
			}

			parryState = ParryState.Inactive;
		}
	}

	public bool Parry => Projectile.ai[0] == 1;

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
		if (InSweetSpot(target, 12))
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
		float mult = 1f - Counter / 5f;

		if (mult > 0)
		{
			const float starScale = 0.8f;

			Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
			Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;

			Vector2 position = GetEndPosition() - Main.screenPosition;

			Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.SteelBlue).Additive() * mult, 0, star.Size() / 2, Projectile.scale * starScale * mult, default);
			Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.White).Additive() * mult, 0, star.Size() / 2, Projectile.scale * 0.8f * starScale * mult, default);
		}

		return false;
	}

	public override bool? CanDamage() => (!Parry && Counter <= 5) ? null : false;

	public bool InSweetSpot(NPC target, int scale)
	{
		float reach = Config.Reach - scale;
		return Projectile.Center.DistanceSQ(target.Hitbox.ClosestPointInRect(Projectile.Center)) > reach * reach;
	}
}