using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Jungle.Toucane;

[AutoloadMinionBuff]
public class ToucaneMinion : BaseMinion
{
	private ref float AiTimer => ref Projectile.ai[0];

	public static readonly Asset<Texture2D> Hand_A = DrawHelpers.RequestLocal(typeof(ToucaneMinion), "ToucaneHand1", false);
	public static readonly Asset<Texture2D> Hand_B = DrawHelpers.RequestLocal(typeof(ToucaneMinion), "ToucaneHand2", false);

	private Vector2 _handPosition_A;
	private Vector2 _handPosition_B;

	public ToucaneMinion() : base(700, 1200, new Vector2(40, 40)) { }

	public override void AbstractSetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override bool PreAI()
	{
		if (_handPosition_A == Vector2.Zero || _handPosition_B == Vector2.Zero)
		{
			_handPosition_A = _handPosition_B = Projectile.Center;
		}

		float sine = (float)Math.Sin(AiTimer / 20f + Projectile.whoAmI);
		_handPosition_A = Vector2.Lerp(_handPosition_A, new Vector2(Projectile.Center.X + 20, Projectile.Center.Y + sine * 5), 0.1f);
		_handPosition_B = Vector2.Lerp(_handPosition_B, new Vector2(Projectile.Center.X - 20, Projectile.Center.Y - sine * 5), 0.1f);

		Projectile.rotation = Projectile.velocity.X * 0.05f;
		AiTimer++;

		return true;
	}

	public override void IdleMovement(Player player)
	{
		float sine = (float)Math.Sin(AiTimer / 30f + Projectile.whoAmI);
		var target = player.MountedCenter + new Vector2(20 * player.direction, -50 + sine * 5);

		if (Projectile.DistanceSQ(target) < 10 && Projectile.velocity.LengthSquared() < 5)
		{
			Projectile.velocity = Vector2.Zero;
		}
		else
		{
			Projectile.AccelFlyingMovement(target, 0.15f, 0.05f, 15);
		}

		Projectile.direction = Projectile.spriteDirection = Player.direction;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		Projectile.direction = Projectile.spriteDirection = (Projectile.Center.X < target.Center.X) ? -1 : 1;
		Projectile.AccelFlyingMovement(target.Center, 0.3f, 0.05f, 14);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Projectile.velocity *= -Main.rand.NextFloat(0.5f, 2f);
		Projectile.netUpdate = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		DrawHand(false);
		Projectile.QuickDraw();
		DrawHand(true);

		return false;

		void DrawHand(bool left)
		{
			var texture = (left ? Hand_B : Hand_A).Value;
			var position = left ? _handPosition_B : _handPosition_A;
			var color = Lighting.GetColor(position.ToTileCoordinates());
			var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		}
	}
}