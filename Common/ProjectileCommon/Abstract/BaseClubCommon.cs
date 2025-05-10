using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract partial class BaseClubProj : ModProjectile
{
	public static readonly SoundStyle DefaultSwing = new("SpiritReforged/Assets/SFX/Item/GenericClubWhoosh")
	{
		Volume = 0.66f
	};

	public static readonly SoundStyle DefaultSmash = new("SpiritReforged/Assets/SFX/Item/GenericClubImpact");

	/// <summary>
	/// Returns the current progress through the club's swing, adjusted for the swing speed multiplier.
	/// </summary>
	public float GetSwingProgress => SwingSpeedMult * _swingTimer / SwingTime;

	/// <summary>
	/// Returns the current progress through the club's windup. Returns 0 if the club has completed the windup and has started charging.
	/// </summary>
	public float GetWindupProgress => (ChargeTime > 0) ? _windupTimer / (float)WindupTime : 0;

	/// <summary>
	/// Returns the scale to draw the club with, using the product of the club's base scale and the melee size modifier set at projectile creation.
	/// </summary>
	public float TotalScale => BaseScale * MeleeSizeModifier;

	/// <summary>
	/// Returns the total amount of radians the club will swing, using the starting angle and the final angle of the swing.
	/// </summary>
	public float AngleRange => SwingAngle_Max - HoldAngle_Final;

	/// <summary>
	/// Checks if the club is fully charge. Use in most instances of checking for charge, for a binary charge rather than a gradient.
	/// </summary>
	public bool FullCharge => Charge == 1;

	/// <summary>
	/// Returns the projectile's owner.
	/// </summary>
	public Player Owner => Main.player[Projectile.owner];

	public enum AIStates
	{
		CHARGING,
		SWINGING,
		POST_SMASH
	}

	/// <summary>
	/// Shorthand form of checking the current AI state of the club, without needing to cast the enumerator back to a float.
	/// </summary>
	/// <param name="checkState"></param>
	/// <returns></returns>
	public bool CheckAIState(AIStates checkState) => AiState == (float)checkState;

	/// <summary>
	/// Shorthand form of changing the AI state of the club without needing to cast the enumerator, in addition to syncing the change.
	/// </summary>
	/// <param name="setState"></param>
	public void SetAIState(AIStates setState)
	{
		AiState = (float)setState;
		Projectile.netUpdate = true;
	}

	/// <summary>
	/// Resets all timers and charge back to 0, in addition to setting the flickered flag to false. <br />
	/// Used for initialization, and for club combos that have multiple chargable swings.
	/// </summary>
	public void ResetData()
	{
		_lingerTimer = LingerTime;
		_swingTimer = 0;
		_windupTimer = 0;
		_flickerTime = 0;
		_hasFlickered = false;
		Charge = 0;
	}

	/// <summary>
	/// Sets the relevant stats of the club weapon to the club projectile.
	/// </summary>
	/// <param name="chargeTime"></param>
	/// <param name="swingTime"></param>
	/// <param name="damageScaling"></param>
	/// <param name="knockbackScaling"></param>
	/// <param name="sizeModifier"></param>
	public void SetStats(int chargeTime, int swingTime, float damageScaling, float knockbackScaling, float sizeModifier)
	{
		ChargeTime = chargeTime;
		SwingTime = swingTime;
		DamageScaling = damageScaling;
		KnockbackScaling = knockbackScaling;
		MeleeSizeModifier = sizeModifier;

		ResetData();
	}

	/// <summary> Translates the club's base rotation to the value needed for drawing the projectile and player arm, without the programmer needing to offset the rotation and account for player direction.
	/// <br/> This does not account for <see cref="Player.fullRotation"/>, which must be calculated independently. </summary>
	private void TranslateRotation(Player owner, out float clubRotation, out float armRotation)
	{
		float output = BaseRotation * owner.gravDir + 1.7f;
		float clubOffset = Pi + PiOver4;
		if (owner.direction < 0)
		{
			output = TwoPi - output;
			clubOffset -= PiOver2;
		}

		clubRotation = output - clubOffset;
		armRotation = output;
	}

	/// <summary>
	/// Returns where the top of the club should draw, rather than the projectile center used for collision. <br />
	/// Can input an offset to move the output backwards down the club
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	public Vector2 GetHeadPosition(Vector2 offset)
	{
		Vector2 handPos = Owner.GetHandRotated();
		float rotation = Projectile.rotation - PiOver4 * Owner.direction;
		if (Owner.direction < 0)
			rotation -= Pi;

		Vector2 directionUnit = rotation.ToRotationVector2();

		return handPos + directionUnit * (Size - offset) * TotalScale;
	}

	public Vector2 GetHeadPosition(float offset = 0) => GetHeadPosition(new Vector2(offset));

	/// <summary>
	/// Quickly does a shockwave circle visual, used primarily for clubs slamming into tiles.
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="size"></param>
	/// <param name="xyRotation"></param>
	/// <param name="opacity"></param>
	internal void DoShockwaveCircle(Vector2 pos, float size, float xyRotation, float opacity)
	{
		var easeFunction = EaseCubicOut;
		float ringWidth = 0.4f;
		int lifetime = 24;
		float zRotation = 0.9f;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(
			pos,
			Color.LightGray * opacity,
			Color.LightGray * opacity,
			ringWidth,
			size * TotalScale,
			lifetime,
			"supPerlin",
			new Vector2(2, 3),
			easeFunction).WithSkew(zRotation, xyRotation).UsesLightColor());
	}

	/// <summary>
	/// Quickly does a dust cloud visual, used primarily for clubs slamming into tiles.
	/// </summary>
	/// <param name="maxClouds"></param>
	internal void DustClouds(int maxClouds, Vector2? positionOverride = null)
	{
		float chargeFactor = Clamp(EaseQuadIn.Ease(Charge), 0.33f, 1f);
		float chargeFactorLerped = Lerp(chargeFactor, 1, 0.5f);

		for (int i = 0; i < maxClouds * chargeFactorLerped; i++)
		{
			Vector2 smokePos = (positionOverride ?? Projectile.Bottom) + Vector2.UnitX * Main.rand.NextFloat(-20, 20);

			float scale = Main.rand.NextFloat(0.05f, 0.07f) * TotalScale;
			scale *= 1 + chargeFactor / 2;

			float speed = Main.rand.NextFloat(4);
			speed *= chargeFactorLerped;
			speed += 0.5f;

			int lifeTime = (int)(40 + Main.rand.Next(-15, 16) * (1 + chargeFactor));

			ParticleHandler.SpawnParticle(new SmokeCloud(smokePos, -Vector2.UnitY * speed, Color.LightGray * 0.75f, scale, EaseQuadOut, lifeTime));
		}
	}

	/// <summary>
	/// Shorthand form for drawing the club's aftertrail, used by default during the swing.
	/// </summary>
	/// <param name="lightColor"></param>
	internal void DrawAftertrail(Texture2D texture, Color lightColor, Vector2 drawPosition)
	{
		Color drawColor = Projectile.GetAlpha(lightColor);

		Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

		for (int k = 0; k < Projectile.oldPos.Length; k++)
		{
			//Don't draw if rotation would be the default value
			if (CheckAIState(AIStates.CHARGING) && Projectile.oldRot[k] == 0)
				continue;

			float progress = (Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length;
			Color trailColor = drawColor * progress * .125f;
			Main.EntitySpriteDraw(texture, drawPosition, frame, trailColor, Projectile.oldRot[k], HoldPoint, TotalScale, Effects, 0);
		}
	}

	/// <summary>
	/// Returns a static function to be used for the club's vertex strip. <br />
	/// Assumes default swinging behavior, if the vertex strip doesn't match the swing, create a different local static function to match it.
	/// </summary>
	/// <param name="Proj"></param>
	/// <returns></returns>
	public static float GetSwingProgressStatic(Projectile Proj) => Proj.ModProjectile is BaseClubProj baseClub ? EaseQuadOut.Ease(baseClub.GetSwingProgress) : 0;
}