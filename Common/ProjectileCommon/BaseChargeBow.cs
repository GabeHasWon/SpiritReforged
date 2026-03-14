using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using System.IO;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Player;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.ProjectileCommon;

public abstract class BaseChargeBow(float maxChargePower = 2f, float perfectShotPower = 1.5f, int perfectShotTime = 30) : ModProjectile
{
	private const int STRING_BOUNCE_TIME = 30; //Could be adjusted to be dynamic if really needed

	protected float Charge { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
	protected float ChargeTime => Projectile.ai[1];  //Set by the item that spawns this projectile, using that item's usetime
	protected float SelectedAmmo => Projectile.ai[2];

	protected readonly float _chargePowerMax = maxChargePower; //The modifier to damage and shot speed when fully charged
	protected readonly float _perfectShotPower = perfectShotPower; //Additional modifier during a perfect shot
	protected readonly float _perfectShotMaxTime = perfectShotTime; //Amount of frames after fully charging that a perfect shot can be performed

	protected int _perfectShotCurTimer = perfectShotTime;
	protected bool _fired = false;
	protected Vector2 _direction = Vector2.Zero;

	public sealed override void SetDefaults()
	{
		Projectile.hostile = false;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.timeLeft = STRING_BOUNCE_TIME;
		SafeSetDefaults();
	}

	public sealed override void AI()
	{
		if (!Projectile.TryGetOwner(out Player player))
		{
			Projectile.Kill();
			return;
		}

		SafeAI();
		AdjustDirection();

		player.ChangeDir(Main.MouseWorld.X > player.position.X ? 1 : -1);
		player.heldProj = Projectile.whoAmI;
		player.itemTime = 2;
		player.itemAnimation = 2;
		Projectile.Center = player.MountedCenter + _direction * 10;
		Projectile.velocity = Vector2.Zero;
		Projectile.rotation = _direction.ToRotation();

		CompositeArmStretchAmount frontStretch = EaseCircularOut.Ease(Charge) switch
		{
			< 0.25f => CompositeArmStretchAmount.Full,
			< 0.5f => CompositeArmStretchAmount.ThreeQuarters,
			< 0.75f => CompositeArmStretchAmount.Quarter,
			_ => CompositeArmStretchAmount.None
		};

		player.SetCompositeArmFront(true, frontStretch, player.itemRotation);
		player.SetCompositeArmBack(true, CompositeArmStretchAmount.Full, player.itemRotation);

		if (!_fired)
		{
			Projectile.timeLeft = STRING_BOUNCE_TIME;
			if (player.channel)
			{
				if (Charge < 1)
					Charge = Min(Charge + 1 / ChargeTime, 1);

				Charging();
			}
			else
			{
				bool perfectShot = Charge == 1 && _perfectShotCurTimer > 0;
				Shoot(perfectShot);
				_fired = true;
				Projectile.netUpdate = true;
			}
		}
		else
			AfterShoot();

		if(Charge == 1)
			_perfectShotCurTimer = (int)Max(--_perfectShotCurTimer, 0);
	}

	public override bool? CanDamage() => false;

	protected void Shoot(bool perfectShot)
	{
		Projectile.TryGetOwner(out Player player);
		Item playerWeapon = player.HeldItem;
		float chargeMod = Lerp(1, _chargePowerMax, Charge);
		if (perfectShot)
			chargeMod *= _perfectShotPower;

		float speed = playerWeapon.shootSpeed * chargeMod;
		int damage = (int)(Projectile.damage * chargeMod);
		float knockBack = Projectile.knockBack * chargeMod;
		int type = GetCurAmmoType();

		var p = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), player.Center, _direction * speed, type, damage, knockBack, Projectile.owner);
		ModifyFiredProj(ref p, Charge == 1, perfectShot);

		OnShoot(perfectShot);
	}

	protected virtual void Charging() { }
	protected virtual void OnShoot(bool perfectShot) { }
	protected virtual void AfterShoot() { }
	protected virtual void SafeSetDefaults() { }
	protected virtual void SafeAI() { }
	protected virtual void ModifyFiredProj(ref Projectile projectile, bool fullCharge, bool perfectShot) { }

	protected virtual int? OverrideProjectile(bool fullCharge, bool perfectShot) => null;

	public abstract void SetStringDrawParams(out float stringLength, out float maxDrawback, out Vector2 stringOrigin, out Color stringColor);

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Type].Value;

		float perfectShotProgress = EaseSine.Ease(EaseCircularOut.Ease(1 - _perfectShotCurTimer / _perfectShotMaxTime));

		//Draw string
		SetStringDrawParams(out float stringLength, out float maxDrawback, out Vector2 stringOrigin, out Color stringColor);

		float stringHalfLength = stringLength/2;
		const float stringScale = 2;
		stringColor = stringColor.MultiplyRGB(lightColor);
		stringColor = Color.Lerp(stringColor, Color.White, perfectShotProgress);

		float timeLeftProgress = 1 - (float)Projectile.timeLeft / STRING_BOUNCE_TIME;
		float easedCharge = EaseCircularOut.Ease(Charge);
		float curDrawback = easedCharge - EaseOutElastic().Ease(timeLeftProgress) * easedCharge;
		curDrawback *= maxDrawback;

		var pointTop = new Vector2(stringOrigin.X, stringOrigin.Y - stringHalfLength);
		var pointMiddle = new Vector2(stringOrigin.X - curDrawback, stringOrigin.Y);
		var pointBottom = new Vector2(stringOrigin.X, stringOrigin.Y + stringHalfLength);
		int splineIterations = 30;
		Vector2[] spline = Spline.CreateSpline([pointTop, pointMiddle, pointBottom], splineIterations);
		for (int i = 0; i < splineIterations; i++)
		{
			var pixelPos = spline[i];

			pixelPos = pixelPos.RotatedBy(Projectile.rotation);
			pixelPos -= (projTex.Size() / 2).RotatedBy(Projectile.rotation);
			pixelPos += Projectile.Center - Main.screenPosition;

			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, pixelPos, new Rectangle(0, 0, 1, 1), stringColor, Projectile.rotation, Vector2.Zero, stringScale, SpriteEffects.None, 0);
		}

		//Draw arrow
		if(!_fired)
		{
			int type = GetCurAmmoType();
			Texture2D arrowTex = TextureAssets.Projectile[type].Value;
			Vector2 arrowPos = pointMiddle.RotatedBy(Projectile.rotation);
			arrowPos -= (projTex.Size() / 2).RotatedBy(Projectile.rotation);
			arrowPos += Projectile.Center - Main.screenPosition;
			var arrowOrigin = new Vector2(arrowTex.Width / 2, arrowTex.Height);

			DrawArrow(arrowTex, arrowPos, arrowOrigin, perfectShotProgress, lightColor);
		}

		//Draw proj
		Projectile.QuickDraw();

		for (int i = 0; i < 2; i++)
			Projectile.QuickDraw(drawColor: Color.White.Additive() * perfectShotProgress);

		return false;
	}
	protected virtual void DrawArrow(Texture2D arrowTex, Vector2 arrowPos, Vector2 arrowOrigin, float perfectShotProgress, Color lightColor)
	{
		Main.spriteBatch.Draw(arrowTex, arrowPos, null, lightColor, Projectile.rotation + PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None, 0);

		for (int i = 0; i < 2; i++)
			Main.spriteBatch.Draw(arrowTex, arrowPos, null, Color.White.Additive() * perfectShotProgress, Projectile.rotation + PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None, 0);
	}

	protected void AdjustDirection(float deviation = 0f)
	{
		Player player = Main.player[Projectile.owner];
		if (Main.myPlayer == player.whoAmI)
		{
			_direction = Main.MouseWorld - (player.Center - new Vector2(4, 4));
			_direction.Normalize();
			_direction = _direction.RotatedBy(deviation);
			Projectile.netUpdate = true;
		}

		player.itemRotation = _direction.ToRotation();
		if (player.direction != 1)
			player.itemRotation -= 3.14f;

		player.itemRotation = WrapAngle(player.itemRotation) - player.direction * PiOver2;
	}

	protected int GetCurAmmoType()
	{
		bool perfectShot = _perfectShotCurTimer < _perfectShotMaxTime && _perfectShotCurTimer > 0;
		bool fullCharge = Charge == 1;
		return OverrideProjectile(fullCharge, perfectShot) ?? (int)SelectedAmmo;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(_fired);
		writer.Write(_perfectShotCurTimer);
		writer.WriteVector2(_direction);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_fired = reader.ReadBoolean();
		_perfectShotCurTimer = reader.ReadInt16();
		_direction = reader.ReadVector2();
	}
}
