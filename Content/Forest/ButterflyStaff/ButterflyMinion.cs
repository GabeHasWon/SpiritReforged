using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using System.Linq;

namespace SpiritReforged.Content.Forest.ButterflyStaff;

[AutoloadMinionBuff()]
public class ButterflyMinion : BaseMinion
{
	public ButterflyMinion() : base(600, 800, new Vector2(16, 16)) { }
	public override void AbstractSetStaticDefaults()
	{
		Main.projFrames[Type] = 2;
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	//public override void SetDefaults() => Projectile.hide = true;

	public override bool PreAI()
	{
		if (AiState == Moving)
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.alpha = Math.Max(Projectile.alpha - 5, 0);

			foreach (Projectile p in Main.projectile.Where(x => x.active && x != null && x.type == Projectile.type && x.owner == Projectile.owner && x != Projectile))
				if (p.Hitbox.Intersects(Projectile.Hitbox))
					Projectile.velocity += Projectile.DirectionFrom(p.Center) / 20;

			if (Main.rand.NextBool(8) && !Main.dedServ)
				ParticleHandler.SpawnParticle(new StarParticle(Projectile.Center + Main.rand.NextVector2Circular(4, 4),
					Projectile.velocity.RotatedByRandom(MathHelper.Pi / 8) * Main.rand.NextFloat(0.2f, 0.4f), Color.LightPink, Color.DeepPink, Main.rand.NextFloat(0.1f, 0.2f), 20));
		}

		else
			Projectile.alpha = Math.Min(Projectile.alpha + 5, 100);

		return true;
	}

	private ref float AiState => ref Projectile.ai[0];
	private const float Moving = 0;
	private const float StuckToPlayer = 1;

	private Vector2 stuckPos = Vector2.Zero;
	public override void IdleMovement(Player player)
	{
		if (AiState == Moving)
		{
			if (Projectile.Distance(player.MountedCenter) > 2000)
				Projectile.Center = player.MountedCenter;

			Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(player.MountedCenter) * 15, 0.04f);
			if (player.Hitbox.Contains(Projectile.Center.ToPoint()))
			{
				stuckPos = Projectile.Center - player.MountedCenter;
				AiState = StuckToPlayer;
				Projectile.netUpdate = true;
			}
		}
		else
			Projectile.Center = stuckPos + player.MountedCenter;
	}

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe)
	{
		framespersecond = 6;
		return true;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		AiState = Moving;

		if (Math.Abs(MathHelper.WrapAngle(Projectile.velocity.ToRotation() - Projectile.AngleTo(target.Center))) < MathHelper.PiOver4) //if close enough in desired angle, accelerate and home accurately
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * 18, 0.1f);

		else //if too much of an angle, circle around
		{
			if (Projectile.velocity.Length() > 8)
				Projectile.velocity *= 0.97f;

			if (Projectile.velocity.Length() < 5)
				Projectile.velocity *= 1.04f;

			Projectile.velocity = Projectile.velocity.Length() * Vector2.Normalize(Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * Projectile.velocity.Length(), 0.125f));
		}
	}

	public override bool MinionContactDamage() => AiState != StuckToPlayer; //Don't deal damage when idling

	public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

	public override bool PreDraw(ref Color lightColor) => false;

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

	public override void PostDraw(Color lightColor)
	{
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.Pink.Additive() * Projectile.Opacity * 0.6f, 0, bloom.Size() / 2, 0.2f, SpriteEffects.None, 0);

		float opacity = AiState == Moving ? 0.6f : 0f;
		Projectile.QuickDrawTrail(Main.spriteBatch, opacity, drawColor: Color.White.Additive(50));
		Projectile.QuickDraw(drawColor: Color.White.Additive(150) * (opacity + 0.4f));
	}
}