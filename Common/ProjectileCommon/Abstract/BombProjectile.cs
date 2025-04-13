﻿using Terraria.Audio;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

/// <summary> Used for building normal bombs like <see cref="ProjectileID.Bomb"/>. </summary>
public abstract class BombProjectile : ModProjectile
{
	public bool DealingDamage { get; private set; }

	/// <summary> The explosion size of this bomb, in tiles. </summary>
	public int area = 5;
	/// <summary> The maximum <see cref="Projectile.timeLeft"/> value, used for visuals. See <see cref="SetTimeLeft"/>. </summary>
	public int timeLeftMax;
	/// <summary> Whether this bomb sticks to tiles according to <see cref="CheckStuck"/>. </summary>
	public bool sticky;

	private int _damage;
	private float _knockback;

	/// <summary> Sets the timeLeft and timeLeftMax values for this projectile, for convenience. </summary>
	protected void SetTimeLeft(int value) => Projectile.timeLeft = timeLeftMax = value;
	/// <summary> Sets the damage and knockback values for this projectile <b>specifically when exploding</b>. </summary>
	protected void SetDamage(int damage, float knockback = 8f)
	{
		_damage = damage;
		_knockback = knockback;
	}

	public override void SetStaticDefaults() => ProjectileID.Sets.Explosive[Type] = true;

	public override void SetDefaults()
	{
		Projectile.friendly = Projectile.hostile = true;
		Projectile.Size = new Vector2(15);
		Projectile.penetrate = -1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		
		SetTimeLeft(180);
		SetDamage(100);
	}

	public override void AI()
	{
		if (sticky && CheckStuck(Projectile.getRect()))
		{
			Projectile.velocity = Vector2.Zero;
		}
		else
		{
			if (Projectile.velocity.Y == 0)
				Projectile.velocity.X *= 0.97f;

			Projectile.velocity.Y += 0.2f;
			Projectile.rotation += Projectile.velocity.X * 0.1f;
		}

		if (!Main.dedServ)
			FuseVisuals();

		if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 3)
		{
			DealingDamage = true;
			Projectile.PrepareBombToBlow();
		}

		Projectile.TryShimmerBounce();
	}

	public static bool CheckStuck(Rectangle area)
	{
		const int padding = 2;
		return Collision.SolidCollision(area.TopLeft() - new Vector2(padding), area.Width + padding * 2, area.Height + padding * 2);
	}

	public virtual void FuseVisuals()
	{
		if (Main.rand.NextBool())
		{
			var position = Projectile.Center - (new Vector2(0, Projectile.height / 2 + 10) * Projectile.scale).RotatedBy(Projectile.rotation);

			var dust = Dust.NewDustPerfect(position, DustID.Smoke, Main.rand.NextVector2Unit(), 100);
			dust.scale = 0.1f + Main.rand.NextFloat(0.5f);
			dust.fadeIn = 1.5f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;

			dust = Dust.NewDustPerfect(position, DustID.Torch, Main.rand.NextVector2Unit(), 100);
			dust.scale = 1f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;
		}
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
		DestroyTiles();
	}

	public sealed override void PrepareBombToBlow()
	{
		int value = area * 16;
		Projectile.Resize(value, value);

		Projectile.damage = _damage;
		Projectile.knockBack = _knockback;
	}

	/// <summary> Destroys tiles in <see cref="area"/>. </summary>
	public void DestroyTiles()
	{
		//Destroy walls and tiles
		var area = new Rectangle((int)(Projectile.Center.X / 16) - this.area / 2, (int)(Projectile.Center.Y / 16) - this.area / 2, this.area, this.area);
		bool doWalls = Projectile.ShouldWallExplode(Projectile.Center, this.area, area.X, area.X + this.area, area.Y, area.Y + this.area);

		Projectile.ExplodeTiles(Projectile.Center, this.area / 2, area.X, area.X + this.area, area.Y, area.Y + this.area, doWalls);
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (!sticky && Projectile.velocity.Y > .05f)
			Projectile.Bounce(oldVelocity, 0.3f);

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDraw();
		return false;
	}
}