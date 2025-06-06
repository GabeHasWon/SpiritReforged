using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Ocean.Items.Rum;

public class RumFire : ModProjectile
{
	public const int TimeLeftMax = 20;
	public bool CanSplit => Projectile.ai[0] > 0;

	public static readonly Vector3 GlowColor = new(0.884f, 0.357f, 0.238f);

	public override void SetStaticDefaults() => Main.projFrames[Type] = 5;
	public override void SetDefaults()
	{
		Projectile.Size = new(20, 34);
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.aiStyle = -1;
		Projectile.penetrate = -1;
		Projectile.timeLeft = TimeLeftMax;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == TimeLeftMax) //Just spawned
		{
			Surface();
		}

		if (Main.rand.NextBool(5))
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
			dust.noGravity = true;
			dust.velocity.Y = -1f;
		}

		if (Main.rand.NextBool(12))
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 150, default, 0.5f);
			dust.fadeIn = 1.25f;
			dust.velocity = new Vector2(0f, Main.rand.Next(-2, -1));
			dust.noLightEmittence = true;
		}

		if (Main.myPlayer == Projectile.owner && Projectile.timeLeft == (int)(TimeLeftMax * 0.75f) && CanSplit)
		{
			Vector2 pos = Projectile.Center + new Vector2(Projectile.velocity.X * 20, 0);
			int damage = Math.Max((int)(Projectile.damage * 0.98f), 1);

			Projectile.NewProjectile(Projectile.GetSource_Death(), pos, Projectile.velocity, ModContent.ProjectileType<RumFire>(), damage, Projectile.knockBack, Projectile.owner, --Projectile.ai[0]);
		}

		Projectile.UpdateFrame((byte)(TimeLeftMax / Main.projFrames[Type]));
		Lighting.AddLight(Projectile.Center, GlowColor);
	}

	private void Surface()
	{
		int surfaceDuration = 0;
		while (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height) || Main.tileSolidTop[Framing.GetTileSafely(Projectile.Bottom).TileType])
		{
			Projectile.position.Y--; //Move up out of solid tiles

			if (TryKill())
				return;
		}

		surfaceDuration = 0;
		while (!Collision.SolidCollision(Projectile.position + Vector2.UnitY, Projectile.width, Projectile.height) && !Main.tileSolidTop[Framing.GetTileSafely(Projectile.Bottom + Vector2.UnitY).TileType])
		{
			Projectile.position.Y++; //Move down onto solid tiles

			if (TryKill())
				return;
		}

		bool TryKill()
		{
			if (++surfaceDuration > 40)
			{
				Projectile.Kill();
				return true;
			}

			return false;
		}
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var effect = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, sizeOffsetY: -2);

		Vector2 position = Projectile.Bottom - Main.screenPosition + new Vector2(0, Projectile.gfxOffY + 2);
		Main.EntitySpriteDraw(texture, position, frame, Color.White, Projectile.rotation, new Vector2(frame.Width / 2, frame.Height), Projectile.scale, effect, 0);
		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.rand.NextBool(3))
			target.AddBuff(BuffID.OnFire, 180);
	}
}

public class RumExplosion : ModProjectile
{
	public const int TimeLeftMax = 20;
	public override void SetStaticDefaults() => Main.projFrames[Type] = 6;

	public override void SetDefaults()
	{
		Projectile.width = Projectile.height = 64;
		Projectile.penetrate = -1;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.friendly = true;
		Projectile.timeLeft = TimeLeftMax;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void AI()
	{
		if (!Main.dedServ)
		{
			Lighting.AddLight(Projectile.Center, RumFire.GlowColor);
			Projectile.UpdateFrame((byte)(TimeLeftMax / Main.projFrames[Type] + 1));
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Projectile.type].Value;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, sizeOffsetY: -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Color.White, Projectile.rotation, source.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
		return false;
	}
}