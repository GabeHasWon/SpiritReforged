using System.IO;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract partial class BaseClubProj : ModProjectile
{
	public virtual float HoldAngle_Intial => PiOver2;
	public virtual float HoldAngle_Final => PiOver4 / 4;
	public virtual float SwingAngle_Max => Pi * 1.75f;

	/// <summary>
	/// As a percentage, how long it takes for the club to wind up before smashing or starting to charge, compared to the amount of time the club takes to charge. <br />
	/// During this time, the player is locked into an animation using the club, even while spam clicking the weapon.
	/// </summary>
	public virtual float WindupTimeRatio => 0.5f;

	/// <summary>
	/// As a percentage, how much of the pullback animation is completed during the windup phase. <br />
	/// A lower number means more of the animation is completed during the windup, meaning the club moves less while charging it.
	/// </summary>
	public virtual float PullbackWindupRatio => 0.33f;

	/// <summary>
	/// As a percentage, how long the club lingers after smashing compared to the amount of time the swing takes to complete.
	/// </summary>
	public virtual float LingerTimeRatio => 1f;

	/// <summary>
	/// Used by the default hold origin for clubs, determines how diagonally far up the held point is, starting from the lower left corner and going to the upper right.
	/// </summary>
	public virtual float HoldPointRatio => 0.1f;

	/// <summary>
	/// The percentage value of progress through the default swing the projectile needs to be at in order to collide with tiles. <br />
	/// Returns 0.25f by default, ie 25% through the swing.
	/// </summary>
	public virtual float SwingPhaseThreshold => 0.25f;

	/// <summary>
	/// The percentage value of progress through the default swing the projectile needs to be at to start shrinking and stop colliding with tiles. <br />
	/// Returns 0.5f by default, ie 50% through the swing.
	/// </summary>
	public virtual float SwingShrinkThreshold => 0.5f;

	/// <summary>
	/// A flat multiplier to how fast the swing completes, ie dividing the effective "SwingTime" <br />
	/// Returns 1f by default, and 1.2f if the swing is fully charged, meaning it completes the swing 20% faster when fully charged.
	/// </summary>
	public virtual float SwingSpeedMult => Charge == 1 ? 1.2f : 1f;

	/// <summary>
	/// A flat multiplier to how fast the charge and windup complete, ie dividing the effective "ChargeTime"
	/// </summary>
	public virtual float ChargeSpeedMult => 1f;

	internal virtual bool AllowUseTurn => CheckAIState(AIStates.CHARGING);

	internal virtual bool AllowRelease => true;

	public virtual void Charging(Player owner)
	{
		if (_windupTimer < WindupTime)
		{
			if (++_windupTimer == WindupTime)
				WindupComplete(owner);
		}
		else
		{
			Charge += ChargeSpeedMult / ChargeTime;
			Charge = Min(Charge, 1);
		}

		if (Charge == 1 && !_hasFlickered)
		{
			ChargeComplete(owner);

			if (!Main.dedServ && _parameters.HasIndicator)
			{
				SoundEngine.PlaySound(Ready, Projectile.Center);
				_flickerTime = MAX_FLICKERTIME;
			}

			_hasFlickered = true;
			Projectile.netUpdate = true;
		}

		float windupAnimProgress = _windupTimer / (float)WindupTime;
		windupAnimProgress = Lerp(windupAnimProgress, Charge, PullbackWindupRatio);

		BaseRotation = ChargedRotationInterpolate(windupAnimProgress);
		BaseScale = ChargedScaleInterpolate(windupAnimProgress);

		--_flickerTime;
	}

	public virtual void Swinging(Player owner)
	{
		float swingProgress = GetSwingProgress;

		bool validTile = Collision.SolidTiles(Projectile.position, Projectile.width, Projectile.height, true);
		BaseScale = 1;

		_swingTimer++;

		BaseRotation = SwingingRotationInterpolate(swingProgress);

		//If the club is touching a tile and isn't currently meant to phase through tiles, do the smash
		if (validTile && CanCollide(swingProgress))
		{
			SetAIState(AIStates.POST_SMASH);
			OnSmash(Projectile.Center);

			if (!Main.dedServ)
			{
				float volume = Clamp(EaseQuadOut.Ease(Charge), 0.66f, 1f);
				SoundEngine.PlaySound(SoundID.Item70.WithVolumeScale(volume), Projectile.Center);
				SoundEngine.PlaySound(DefaultSmash.WithVolumeScale(volume), Projectile.Center);

				if (Main.LocalPlayer == owner)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.Normalize(Projectile.oldPosition - Projectile.position), 1 + Charge * 2, 6, (int)(20 * (0.5f + Charge / 2))));
			}
		}

		if (swingProgress >= SwingShrinkThreshold)
		{
			float shrinkProgress = (swingProgress - SwingShrinkThreshold) / (1 - SwingShrinkThreshold);
			shrinkProgress = Clamp(shrinkProgress, 0, 1);

			BaseScale = Lerp(1, 0, EaseCubicIn.Ease(shrinkProgress));

			if (swingProgress > 1)
				Projectile.Kill();
		}
	}

	public virtual void AfterCollision()
	{
		const float shrinkThreshold = 0.6f;

		_lingerTimer--;
		float lingerProgress = _lingerTimer / (float)LingerTime;
		lingerProgress = 1 - lingerProgress;

		float shrinkProgress = (lingerProgress - shrinkThreshold) / (1 - shrinkThreshold);
		shrinkProgress = Clamp(shrinkProgress, 0, 1);

		BaseScale = Lerp(1, 0, EaseCubicOut.Ease(EaseCircularIn.Ease(shrinkProgress)));

		if (_lingerTimer <= 0)
			Projectile.Kill();

		BaseRotation += Lerp(-0.05f, 0.05f, EaseQuadIn.Ease(lingerProgress)) * (1 + Charge / 2);
	}

	public void KillAndStopAnimation()
	{
	}

	/// <summary>
	/// Determines the rate at which the projectile's scale grows during the default charging + windup behavior.<br />
	/// Override to change how the scale interpolates without overriding the rest of the behavior.
	/// </summary>
	/// <param name="progress"></param>
	/// <returns></returns>
	internal virtual float ChargedScaleInterpolate(float progress) => Lerp(0f, 1f, EaseQuadInOut.Ease(EaseCircularOut.Ease(progress)));

	/// <summary>
	/// Determines the rate at which the projectile's rotation moves backwards during the default charging + windup behavior.<br />
	/// Override to change how the rotation interpolates without overriding the rest of the behavior.
	/// </summary>
	/// <param name="progress"></param>
	/// <returns></returns>
	internal virtual float ChargedRotationInterpolate(float progress) => Lerp(HoldAngle_Intial, HoldAngle_Final, EaseCircularOut.Ease(progress));

	/// <summary>
	/// Determines the rate at which the projectile's rotation moves forwards during the default swinging behavior.<br />
	/// Override to change how the rotation interpolates without overriding the rest of the behavior.
	/// </summary>
	/// <param name="progress"></param>
	/// <returns></returns>
	internal virtual float SwingingRotationInterpolate(float progress) => Lerp(HoldAngle_Final, SwingAngle_Max, EaseQuadOut.Ease(progress));

	/// <summary>
	/// Determines whether or not the projectile is currently allowed to collide with tiles during the default swinging behavior.<br />
	/// Override to change the condition without overriding the rest of the behavior.
	/// </summary>
	/// <param name="progress"></param>
	/// <returns></returns>
	internal virtual bool CanCollide(float progress) => progress > SwingPhaseThreshold && progress < SwingShrinkThreshold;

	public virtual void SafeSetStaticDefaults() { }
	public virtual void SafeSetDefaults() { }
	public virtual void SafeAI() { }
	public virtual void SafeDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition) { }

	internal virtual void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) { }
	internal virtual void SafeModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) { }

	internal virtual void DuringCharge(Player owner, float windupProgress, float chargeProgress) { }

	internal virtual void WindupComplete(Player owner) { }

	internal virtual void ChargeComplete(Player owner) { }

	public virtual void OnSwingStart() { }
	public virtual void OnSmash(Vector2 position) { }
	public virtual bool OverrideDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition) => false;
	public virtual bool AllowedAftertrailDraw(ref float trailOpacity) => CheckAIState(AIStates.SWINGING);

	internal virtual void SendExtraDataSafe(BinaryWriter writer) { }
	internal virtual void ReceiveExtraDataSafe(BinaryReader reader) { }

	public virtual SpriteEffects Effects => Owner.direction * (int)Owner.gravDir < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
	public virtual Vector2 HoldPoint => Effects == SpriteEffects.FlipHorizontally ? Size * (1 - HoldPointRatio) : new Vector2(Size.X * HoldPointRatio, Size.Y * (1 - HoldPointRatio));
}