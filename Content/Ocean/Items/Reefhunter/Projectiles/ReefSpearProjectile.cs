using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using static Terraria.Player;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

public class ReefSpearProjectile : ModProjectile
{
	private const int MAX_DISTANCE = 70;
	private const int NUM_STABS = 3;
	private const float WINDUP_TIME = 0.3f; //Percentage of the full lifetime, 0.3f is 30%

	public Vector2 RealDirection => (_direction * MAX_DISTANCE).RotatedBy(_rotationOffset * _rotationDirection);

	private Vector2 _direction = Vector2.Zero;
	private int _maxTimeleft = 0;
	private int _realMaxTimeleft = 0;
	private float _rotationOffset = 0;
	private float _rotationDirection = 0;
	private bool _canHitEnemy = false;
	private bool _hitEffectCooldown = false;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.ReefSpear.DisplayName");

	public override void SetDefaults()
	{
		Projectile.width = 46;
		Projectile.height = 46;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.ownerHitCheck = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.aiStyle = -1;
		Projectile.timeLeft = 70;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.ownerHitCheck = true;
		DrawHeldProjInFrontOfHeldItemAndArms = false;

		MoRHelper.SetSpearBonus(Projectile);
	}

	public override bool? CanDamage() => true;

	public override void AI()
	{
		Player p = Main.player[Projectile.owner];
		p.heldProj = Projectile.whoAmI;
		p.itemTime = 2;
		p.itemAnimation = 2;
		p.reuseDelay = 15;
		CompositeArmStretchAmount stretchAmount = CompositeArmStretchAmount.Full;
		float factor = 1; //Lerp factor for pushing out and coming back in
		float length = 1;
		_canHitEnemy = false;

		if (_maxTimeleft == 0) //Initialize
		{
			if (p.whoAmI == Main.myPlayer)
				_direction = Vector2.Normalize(Main.MouseWorld - p.Center);

			float modifiedAttackSpeed = 0.25f + p.GetWeaponAttackSpeed(p.HeldItem) * 0.75f;
			Projectile.timeLeft = (int)(Projectile.timeLeft / modifiedAttackSpeed);
			_realMaxTimeleft = Projectile.timeLeft;
			_maxTimeleft = (int)(_realMaxTimeleft * (1 - WINDUP_TIME));
			_rotationOffset = MathHelper.Pi * 0.13f * (Main.rand.NextBool() ? -1 : 1);
			Projectile.netUpdate = true;
		}

		if (Projectile.timeLeft > _maxTimeleft)
			Windup(p, ref factor, ref stretchAmount);
		else
			StabCombo(ref length, ref factor, ref stretchAmount);

		p.SetCompositeArmFront(true, stretchAmount, _direction.ToRotation() - 1.57f + _rotationOffset * _rotationDirection);
		var clampedStretch = (CompositeArmStretchAmount)MathHelper.Clamp((byte)stretchAmount, 1, 2);
		p.SetCompositeArmBack(true, clampedStretch, _direction.ToRotation() - 1.57f + _rotationOffset * _rotationDirection);

		var offset = new Vector2(0, -2 * p.direction);

		Projectile.Center = p.Center + new Vector2(0, p.gfxOffY) - Vector2.Lerp(RealDirection, Vector2.Zero, factor * length) + RealDirection * 0.5f + offset;
	}

	private void Windup(Player p, ref float factor, ref CompositeArmStretchAmount stretchAmount)
	{
		float progress = (float)(Projectile.timeLeft - _maxTimeleft) / (_realMaxTimeleft - _maxTimeleft);
		progress = EaseFunction.EaseQuadOut.Ease(1 - progress);
		factor = MathHelper.Lerp(0.25f, 0f, progress);
		if (p.whoAmI == Main.myPlayer)
		{
			_direction = Vector2.Normalize(Main.MouseWorld - p.Center);
			p.direction = Main.MouseWorld.X >= p.MountedCenter.X ? 1 : -1;
		}

		if (progress > 0.5f)
			stretchAmount = CompositeArmStretchAmount.Quarter;

		Projectile.netUpdate = true;
	}

	private void StabCombo(ref float length, ref float factor, ref CompositeArmStretchAmount stretchAmount)
	{
		_canHitEnemy = true;
		float progress = Projectile.timeLeft / (float)_maxTimeleft;
		float progressExponent = 2f;
		List<int> stabTimes = [];
		for (int i = NUM_STABS; i > 0; i--)
			stabTimes.Add((int)(Math.Pow(i / (float)NUM_STABS, 1 / progressExponent) * _maxTimeleft));

		progress = (float)(Math.Pow(progress, progressExponent));
		length = Projectile.timeLeft <= stabTimes[2] ? 1f : 0.65f;

		for (int i = 0; i < stabTimes.ToArray().Length; i++)
		{
			if (Projectile.timeLeft == stabTimes[i])
			{
				if (_rotationDirection == 0)
					_rotationDirection = 1;

				_rotationDirection *= Main.rand.NextFloat(-.7f, -.5f);
				if (Projectile.timeLeft == stabTimes[NUM_STABS - 1])
					_rotationDirection = 0;

				Projectile.ResetLocalNPCHitImmunity();
				_hitEffectCooldown = false;
				Projectile.netUpdate = true;

				//do vfx and sfx hereq

				if (!Main.dedServ)
				{
					SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { PitchVariance = 0.3f, Volume = 0.75f, MaxInstances = -1 }, Projectile.Center);
					int particleLifetime = stabTimes[i] + 7;
					if (i < stabTimes.ToArray().Length - 1)
						particleLifetime -= stabTimes[i + 1];

					Vector2 particleVelocity = RealDirection / particleLifetime;
					particleVelocity *= -1.5f;

					ParticleHandler.SpawnParticle(new ReefSpearImpact(
						Projectile,
						Projectile.Center - particleVelocity,
						particleVelocity,
						200 * length,
						3f * MAX_DISTANCE,
						_direction.ToRotation() + _rotationOffset * _rotationDirection,
						particleLifetime,
						0.8f,
						6));
				}
			}
		}

		factor = 1 - progress % (1 / (float)NUM_STABS) * NUM_STABS;
		factor = (float)Math.Pow(factor, 0.75f);
		factor = (float)Math.Sin(factor * MathHelper.Pi);
		factor = EaseFunction.EaseQuadIn.Ease(factor);

		//Stretch arm based on distance from player
		if (factor < 0.66f)
			stretchAmount = CompositeArmStretchAmount.Quarter;
		else if (factor < 0.33f)
			stretchAmount = CompositeArmStretchAmount.ThreeQuarters;
	}

	public override bool ShouldUpdatePosition() => false;
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = Math.Sign(_direction.X);
	public override void ModifyHitPlayer(Player target, ref HurtModifiers modifiers) => modifiers.HitDirectionOverride = Math.Sign(_direction.X);

	private bool CheckStuckSpears(NPC target)
	{
		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.ModProjectile == null)
				continue;

			if (proj.ModProjectile is ReefSpearThrown reefSpear && proj.owner == Projectile.owner)
				return reefSpear.GetStuckNPC() == target;
		}

		return false;
	}

	public override void ModifyDamageHitbox(ref Rectangle hitbox)
	{
		Vector2 pos = Projectile.Center + RealDirection - Projectile.Size / 2;

		hitbox.X = (int)pos.X;
		hitbox.Y = (int)pos.Y;
	}

	public override bool? CanHitNPC(NPC target) => _canHitEnemy ? null : false;
	public override bool CanHitPlayer(Player target) => _canHitEnemy;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		HitEffects();

		if (CheckStuckSpears(target))
			target.SimpleStrikeNPC(damageDone / 3, hit.HitDirection, false, 0, null, false, 0, false);
	}

	public override void OnHitPlayer(Player target, HurtInfo info) => HitEffects();

	private void HitEffects()
	{
		if (_hitEffectCooldown)
			return;

		float scaleMod = (_rotationDirection == 0) ? 1f : 0.66f;
		Vector2 particleBasePos = Projectile.Center + RealDirection;

		var particle = new TexturedPulseCircle(
				particleBasePos,
				new Color(230, 27, 112) * 0.5f,
				0.75f,
				200 * scaleMod,
				(int)(35 * scaleMod),
				"noise",
				new Vector2(4, 0.75f),
				EaseFunction.EaseCubicOut).WithSkew(0.75f, _direction.ToRotation() + _rotationOffset * _rotationDirection - MathHelper.Pi).UsesLightColor();
		particle.Velocity = Vector2.Normalize(RealDirection) / 2;

		ParticleHandler.SpawnParticle(particle);

		for (int i = 0; i < (int)(Main.rand.Next(5, 7) * scaleMod); i++)
		{
			Vector2 offset = Vector2.UnitY.RotatedBy(_direction.ToRotation() + _rotationOffset * _rotationDirection) * Main.rand.NextFloat(-15, 15) * scaleMod;
			offset = offset.RotatedByRandom(0.2f);
			ParticleHandler.SpawnParticle(new BubbleParticle(particleBasePos + offset, Vector2.Normalize(RealDirection) * Main.rand.NextFloat(1f, 4f) * scaleMod, Main.rand.NextFloat(0.1f, 0.2f), Main.rand.Next(30, 61)));
		}

		_hitEffectCooldown = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D t = TextureAssets.Projectile[Projectile.type].Value;
		Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, lightColor, RealDirection.ToRotation(), new Vector2(40, 14), 1f, SpriteEffects.None, 1f);
		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.WriteVector2(_direction);
		writer.Write(_maxTimeleft);
		writer.Write(_realMaxTimeleft);
		writer.Write(_rotationOffset);
		writer.Write(_rotationDirection);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_direction = reader.ReadVector2();
		_maxTimeleft = reader.ReadInt32();
		_realMaxTimeleft = reader.ReadInt32();
		_rotationOffset = reader.ReadSingle();
		_rotationDirection = reader.ReadSingle();
	}
}