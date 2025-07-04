using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Ocean.Items.Rum;

public class RumFire : ModProjectile
{
	public const int TimeLeftMax = 20;
	public bool CanSplit => Projectile.ai[0] > 0;

	public static readonly Vector3 GlowColor = new(0.884f, 0.357f, 0.238f);

	public override void SetStaticDefaults() => Main.projFrames[Type] = 5;
	public override void SetDefaults()
	{
		Projectile.Size = new(16, 16);
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.aiStyle = -1;
		Projectile.penetrate = -1;
		Projectile.timeLeft = TimeLeftMax;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.Opacity = 0;
	}

	public override void AI()
	{
		if (!Projectile.Surface())
		{
			Projectile.Kill();
			return;
		}

		Projectile.Opacity = Math.Min(Projectile.Opacity + 0.1f, 1);

		if (Main.rand.NextBool(5))
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
			dust.noGravity = true;
			dust.velocity.Y = -1f;
		}

		if (Main.rand.NextBool(15))
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 150, default, 0.35f);
			dust.fadeIn = 1.25f;
			dust.velocity = new Vector2(0f, Main.rand.Next(-2, -1));
			dust.noLightEmittence = true;
		}

		if (Main.rand.NextBool(30))
		{
			var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f);
			var velocity = (Vector2.UnitY * -Main.rand.NextFloat(1.5f)).RotatedByRandom(0.25f);

			ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.OrangeRed, Main.rand.NextFloat(0.4f), 120, 5));
		}
		 
		if (Main.rand.NextBool(6))
		{
			var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f);
			var velocity = (Vector2.UnitY * -Main.rand.NextFloat(2, 4.5f)).RotatedByRandom(0.25f);

			Color[] fireColors = [Color.Yellow.Additive(150), Color.Orange.Additive(150), Color.Red.Additive(150) * 0.85f];
			float intensity = 1.5f * Projectile.Opacity;
			float scale = Main.rand.NextFloat(0.05f, 0.1f);
			int maxTime = Main.rand.Next(20, 60);
			ParticleHandler.SpawnParticle(new FireParticle(position, velocity, fireColors, intensity, scale, EaseFunction.EaseCircularOut, maxTime)
			{
				ColorLerpExponent = 2.5f,
				PixelDivisor = 2
			});
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

	public override bool ShouldUpdatePosition() => false;
	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var effect = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, sizeOffsetY: -2);

		Vector2 position = Projectile.Bottom - Main.screenPosition + new Vector2(0, Projectile.gfxOffY + 2);
		Main.EntitySpriteDraw(texture, position, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, new Vector2(frame.Width / 2, frame.Height), Projectile.scale, effect, 0);
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
            Projectile.UpdateFrame((byte)(TimeLeftMax / Main.projFrames[Type]));

            if (Main.rand.NextBool(2))
			{
				var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f);
				var velocity = (Vector2.UnitY * -Main.rand.NextFloat(1.5f)).RotatedByRandom(0.25f);

				ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.OrangeRed, Main.rand.NextFloat(0.4f), 120, 2));
				ParticleHandler.SpawnParticle(new SmokeCloud(position, velocity, Color.DarkGray * .3f, Main.rand.NextFloat(.25f), Common.Easing.EaseFunction.EaseCircularOut, 60));
			}
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