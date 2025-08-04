using SpiritReforged.Common.Easing;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract class SwungProjectile : ModProjectile
{
	public readonly record struct Configuration(EaseFunction Easing, int Reach, int Width);

	/// <summary> The full duration of the swing. </summary>
	public float SwingTime => Main.player[Projectile.owner].itemTimeMax;
	/// <summary> The progress of the swing relative to <see cref="SwingTime"/>. </summary>
	public float Progress => Counter / SwingTime;
	/// <summary> The absolute direction of the swing. </summary>
	public int SwingDirection => Projectile.direction * Math.Sign(SwingArc);

	/// <summary> The full arc of the swing in radians. </summary>
	public float SwingArc;
	/// <summary> The progress of the swing. </summary>
	public int Counter;

	protected Configuration config;

	/// <summary><inheritdoc cref="ModProjectile.SetDefaults"/><para/>
	/// Prefer overriding <see cref="SetConfiguration"/> instead of this method. </summary>
	public sealed override void SetDefaults()
	{
		Projectile.Size = new Vector2(18);
		Projectile.DamageType = DamageClass.Melee;
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;

		config = SetConfiguration();
	}

	/// <summary><inheritdoc cref="ModProjectile.SetDefaults"/><para/>
	/// Must return a custom <see cref="Configuration"/>. </summary>
	public abstract Configuration SetConfiguration();

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];

		Projectile.spriteDirection = Projectile.direction = owner.direction = (Projectile.velocity.X > 0) ? 1 : -1;
		Projectile.rotation = GetRotation(out float armRotation);
		Projectile.Center = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
		owner.heldProj = Projectile.whoAmI;

		if (++Counter < SwingTime - 2)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;
	}

	/// <summary> Gets the rotation of the swing aside from texture orientation. Used primarily for collision checks. </summary>
	public float GetAbsoluteAngle()
	{
		float ease = config.Easing.Ease(MathHelper.Min(Progress, 1));
		float progress = (Projectile.direction == -1) ? 1f - ease : ease;

		return Projectile.velocity.ToRotation() - SwingArc / 2f + SwingArc * progress;
	}

	public virtual float GetRotation(out float armRotation)
	{
		float value = GetAbsoluteAngle();
		armRotation = value - 1.57f;

		return value;
	}

	public override void CutTiles() => Projectile.PlotTileCut(config.Reach, Projectile.width);
	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		var endPos = Projectile.Center + (Vector2.UnitX * config.Reach).RotatedBy(GetAbsoluteAngle());
		float collisionPoint = 0f;

		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, endPos, config.Width, ref collisionPoint);
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(SwingArc);
		writer.Write(Counter);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		SwingArc = reader.ReadSingle();
		Counter = reader.ReadInt32();
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
		var origin = new Vector2(4, (effects == SpriteEffects.FlipVertically) ? 9 : 30); //The handle
		var color = Projectile.GetAlpha(lightColor) * Math.Min(Counter / SwingTime * 3, 1) * 0.5f;

		DrawHeld(lightColor, origin, Projectile.rotation * SwingDirection, effects);
		DrawSmear(color, Projectile.rotation - SwingArc * 0.5f * Projectile.spriteDirection, effects);

		return false;
	}

	#region helper methods
	public void DrawSmear(Color color, float rotation, SpriteEffects effects = default) => DrawSmear(color, rotation, (int)(Progress * 12f), config.Reach + 10, effects: effects);
	public void DrawSmear(Color color, float rotation, int frame, float distance, float scale = 0.75f, SpriteEffects effects = default)
	{
		Main.instance.LoadProjectile(985);
		var smear = TextureAssets.Projectile[985].Value;

		var player = Main.player[Projectile.owner];
		var source = smear.Frame(1, 4, 0, frame);
		var position = player.Center + (Vector2.UnitX * distance).RotatedBy(rotation) - Main.screenPosition;

		Main.EntitySpriteDraw(smear, position, source, color, rotation, new Vector2(source.Width, source.Height / 2), scale, effects, 0);
	}

	public void DrawHeld(Color color, Vector2 origin, float rotation, SpriteEffects effects = default)
	{
		var texture = TextureAssets.Projectile[Type];
		float visCounter = MathHelper.Min(Counter / (SwingTime / 2), 1);
		var frame = texture.Frame(1, Main.projFrames[Type], 0, (int)(visCounter * (Main.projFrames[Type] - 1)), 0, -2);
		var position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);

		Main.EntitySpriteDraw(texture.Value, position, frame, color, rotation, origin, Projectile.scale, effects);
	}

	public static Projectile Spawn(Vector2 position, Vector2 velocity, int type, int damage, float knockback, Player owner, float swingArc, IEntitySource source = default, float ai0 = 0, float ai1 = 0, float ai2 = 0)
	{
		if (source == default)
		{
			var item = owner.HeldItem;
			source = owner.GetSource_ItemUse_WithPotentialAmmo(item, item.useAmmo);
		}

		return PreNewProjectile.New(source, position, velocity, type, damage, knockback, owner.whoAmI, ai0, ai1, ai2, (p) => (p.ModProjectile as SwungProjectile).SwingArc = swingArc);
	}
	#endregion
}