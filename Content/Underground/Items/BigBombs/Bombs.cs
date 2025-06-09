using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.BigBombs;

public class Bomb : BombProjectile, ILargeExplosive
{
	public const int CommonSize = 32;

	public virtual int OriginalType => ProjectileID.Bomb;
	public override LocalizedText DisplayName => Language.GetText("ProjectileName.Bomb");

	public sealed override void SetDefaults()
	{
		base.SetDefaults();

		Projectile.Size = new Vector2(CommonSize);
		Projectile.scale = 0;
		SetDamage(150);
		area = 15;

		PostSetDefaults();
	}

	public virtual void PostSetDefaults() { }

	public override void AI()
	{
		base.AI();

		if (!DealingDamage)
		{
			float oldScale = Projectile.scale; //Resize logic
			Projectile.scale = Math.Min(Projectile.scale + .1f, 1);

			if (Projectile.scale != oldScale)
			{
				int size = (int)Math.Max(CommonSize * Projectile.scale, 2);
				Projectile.Resize(size, size);
			}
		}
	}

	public override void OnKill(int timeLeft)
	{
		DestroyTiles();

		if (Main.dedServ)
			return;

		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
		SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
		var ease = new PolynomialEase((float x) => (float)(0.5 + 0.5 * Math.Pow(x, 0.5)));
		var stretch = Vector2.One;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.Goldenrod.Additive(), Color.OrangeRed.Additive(), 1f, 30 * area, 20, "Smoke", stretch, ease)
		{
			Angle = Main.rand.NextFloat(-MathHelper.TwoPi, MathHelper.TwoPi)
		});

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.White.Additive(), Color.OrangeRed.Additive(), .5f, 30 * area, 20, "Smoke", stretch, ease)
		{
			Angle = Main.rand.NextFloat(-MathHelper.TwoPi, MathHelper.TwoPi)
		});

		ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, Vector2.Zero, Color.Gray, .04f * area, EaseFunction.EaseCubicOut, 40));

		const int time = 5;
		ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Color.Orange.Additive(), new Vector2(0.2f, 1f) * area, time));
		ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Color.White.Additive(), new Vector2(0.1f, 1f) * area, time));

		for (int i = 0; i < area * 2; i++)
		{
			float magnitude = Main.rand.NextFloat();

			var color = Color.OrangeRed.Additive();
			var velocity = Main.rand.NextVector2Unit() * magnitude * 10f;
			float scale = (1f - magnitude) * 0.08f * area;

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, color, scale, 10, 3));
			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, Color.White.Additive(), scale * .5f, 10, 3));

			var d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16f * area), DustID.Torch, Scale: Main.rand.NextFloat() + .5f);
			d.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var origin = new Vector2(texture.Width / 2, texture.Height / 2 + 4);

		float lerp = GetLerp();
		float scale = Projectile.scale + lerp * .1f;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, SpriteEffects.None);

		var color = Projectile.GetAlpha(Color.Red.Additive()) * lerp * 2;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, scale, SpriteEffects.None);
		return false;
	}

	public float GetLerp()
	{
		float progress = 1f - (float)Projectile.timeLeft / timeLeftMax;
		return (float)Math.Sin(Projectile.timeLeft * 8f / (60f - progress * 20)) * progress;
	}
}

public class BombDirt : SpreadBomb, ILargeExplosive
{
	public virtual int OriginalType => ProjectileID.DirtBomb;
	public override LocalizedText DisplayName => Language.GetText("ProjectileName.Bomb");

	public sealed override void SetDefaults()
	{
		base.SetDefaults();

		Projectile.Size = new Vector2(Bomb.CommonSize);
		SetDamage(150);
		area = 6;
		dustType = DustID.Dirt;
		tileType = TileID.Dirt;

		PostSetDefaults();
	}

	public virtual void PostSetDefaults() { }

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var origin = new Vector2(texture.Width / 2, texture.Height / 2 + 4);

		float lerp = GetLerp();
		float scale = Projectile.scale + lerp * .1f;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, SpriteEffects.None);

		var color = Projectile.GetAlpha(Color.Red.Additive()) * lerp * 2;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, scale, SpriteEffects.None);
		return false;
	}

	public float GetLerp()
	{
		float progress = 1f - (float)Projectile.timeLeft / timeLeftMax;
		return (float)Math.Sin(Projectile.timeLeft * 8f / (60f - progress * 20)) * progress;
	}
}

public class BombClay : BombDirt
{
	public override int OriginalType => ModContent.ProjectileType<ClayBombProjectile>();

	public override void PostSetDefaults()
	{
		dustType = DustID.Clay;
		tileType = TileID.ClayBlock;
	}
}

public class BombSand : BombDirt
{
	public override int OriginalType => ModContent.ProjectileType<SandBombProjectile>();

	public override void PostSetDefaults()
	{
		dustType = DustID.Sand;
		tileType = TileID.Sand;
	}
}

public class BombMud : BombDirt
{
	public override int OriginalType => ModContent.ProjectileType<MudBombProjectile>();

	public override void PostSetDefaults()
	{
		dustType = DustID.Mud;
		tileType = TileID.Mud;
	}
}

public class BombCoarse : BombDirt
{
	public override int OriginalType => ModContent.ProjectileType<CoarseBombProjectile>();

	public override void PostSetDefaults()
	{
		dustType = DustID.Dirt;
		tileType = ModContent.TileType<SavannaDirt>();
	}
}

public class BombSticky : Bomb
{
	public override int OriginalType => ProjectileID.StickyBomb;

	public override void PostSetDefaults() => sticky = true;
}

public class BombBouncy : Bomb
{
	public override int OriginalType => ProjectileID.BouncyBomb;

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Projectile.Bounce(oldVelocity);
		return false;
	}
}

public class BombDirtSticky : BombDirt
{
	public override int OriginalType => ProjectileID.DirtStickyBomb;

	public override void PostSetDefaults() => sticky = true;
}

public class BombClaySticky : BombClay
{
	public override int OriginalType => ModContent.ProjectileType<ClayBombStickyProjectile>();

	public override void PostSetDefaults()
	{
		base.PostSetDefaults();
		sticky = true;
	}
}

public class BombSandSticky : BombSand
{
	public override int OriginalType => ModContent.ProjectileType<SandBombStickyProjectile>();

	public override void PostSetDefaults()
	{
		base.PostSetDefaults();
		sticky = true;
	}
}

public class BombMudSticky : BombMud
{
	public override int OriginalType => ModContent.ProjectileType<MudBombStickyProjectile>();

	public override void PostSetDefaults()
	{
		base.PostSetDefaults();
		sticky = true;
	}
}

public class BombCoarseSticky : BombCoarse
{
	public override int OriginalType => ModContent.ProjectileType<CoarseBombStickyProjectile>();

	public override void PostSetDefaults()
	{
		base.PostSetDefaults();
		sticky = true;
	}
}

public class BombFish : Bomb
{
	public override int OriginalType => ProjectileID.BombFish;

	public override void PostSetDefaults() => sticky = true;
}

[AutoloadGlowmask("255,255,255", false)]
public class BombScarab : Bomb
{
	public override int OriginalType => ProjectileID.ScarabBomb;

	private bool _wasAttached;

	public override void PostSetDefaults()
	{
		area = 5;
		sticky = true;
	}

	public override void AI()
	{
		if (CheckStuck(Projectile.getRect()))
		{
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation = Projectile.AngleFrom(Main.player[Projectile.owner].Center) - MathHelper.PiOver2;

			var position = Projectile.Center - (new Vector2(0, Projectile.height / 2 + 10) * Projectile.scale).RotatedBy(Projectile.rotation);
			for (int i = 0; i < 2; i++)
			{
				var d = Dust.NewDustPerfect(position, DustID.BlueTorch, position.DirectionFrom(Projectile.Center).RotatedByRandom(0.5f), Scale: Main.rand.NextFloat() + 1.5f);
				d.noGravity = true;
			}

			if (!_wasAttached)
			{
				for (int i = 0; i < 15; i++)
				{
					var vel = new Vector2(Main.rand.NextFloat(3f) * (Main.rand.NextBool() ? -1 : 1), 0).RotatedBy(Projectile.rotation);
					var d = Dust.NewDustPerfect(position, DustID.BlueTorch, vel, Scale: Main.rand.NextFloat() + 1.5f);
					d.noGravity = true;
					d.fadeIn = 1.25f;
				}
			}

			_wasAttached = true;
		}
		else
		{
			base.AI();
			_wasAttached = false;
		}
	}

	public override void FuseVisuals()
	{
		var position = Projectile.Center - (new Vector2(0, Projectile.height / 2 + 10) * Projectile.scale).RotatedBy(Projectile.rotation);

		var d = Dust.NewDustPerfect(position, DustID.BlueTorch, Vector2.Zero, Scale: Main.rand.NextFloat() + 1);
		d.noGravity = true;
	}

	public override bool OnTileCollide(Vector2 oldVelocity) => false;

	public override void OnKill(int timeLeft)
	{
		var scarabBombDigDirectionSnap = ScarabBombSnap();
		float angle = scarabBombDigDirectionSnap.ToVector2().ToRotation() + (float)Math.PI / 2f;

		if (!Main.dedServ) //VFX
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			Projectile.Resize(80, 80);

			for (int i = 0; i < 60; i++)
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
				dust.velocity.Y -= 0.5f;

				var dust2 = dust;
				dust2.velocity *= 1.2f;
				dust.color = Color.Black * 0.9f;

				if (Main.rand.NextBool(2))
				{
					dust.scale = 0.5f;
					dust.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
					dust.color = Color.Black * 0.8f;
				}
			}

			for (int num940 = 0; num940 < 30; num940++)
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f, 100);
				dust.noGravity = true;
				if (Math.Abs(Projectile.velocity.X) > 0.25f)
				{
					Projectile.velocity.X *= 0.25f / Math.Abs(Projectile.velocity.X);
				}

				dust.velocity.Y -= 0.5f;
				dust.fadeIn = 1.2f;
				var dust2 = dust;
				dust2.velocity *= 8f;
				dust.velocity = dust.velocity.RotatedBy(angle);
				dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f, 100, default, 1.5f);
				dust2 = dust;
				dust2.velocity *= 2f;
				dust.velocity = dust.velocity.RotatedBy(angle);
				dust.velocity.Y -= 1.5f;
				dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f, 100, default, 1.5f);
				dust.noGravity = true;
				dust.velocity.Y -= 1f;
				dust.fadeIn = 2f;
				dust2 = dust;
				dust2.velocity *= 4f;
				dust.velocity = dust.velocity.RotatedBy(angle);
			}

			bool flag4 = Math.Abs(scarabBombDigDirectionSnap.X) + Math.Abs(scarabBombDigDirectionSnap.Y) == 1;
			for (int num941 = 1; num941 <= 3; num941++)
			{
				float num942 = (float)Math.PI * 2f * Main.rand.NextFloat();
				for (float num943 = 0f; num943 < 1f; num943 += 1f / 12f)
				{
					float f6 = (float)Math.PI * 2f * num943 + num942;
					Vector2 value13 = f6.ToRotationVector2();
					value13 *= new Vector2(0.7f, 0.3f);
					var dust = Dust.NewDustPerfect(Projectile.Center, 59, value13);
					dust.fadeIn = 2f;
					dust.noGravity = true;
					Dust dust2 = dust;
					dust2.velocity *= (float)num941 + Main.rand.NextFloat() * 0.6f;
					dust.velocity.Y -= (float)num941 * 0.8f;
					dust.velocity = dust.velocity.RotatedBy(angle);
					if (flag4)
					{
						dust2 = dust;
						dust2.velocity += scarabBombDigDirectionSnap.ToVector2() * (Main.rand.NextFloat() * 11f + 6f);
					}
					else
					{
						dust2 = dust;
						dust2.velocity += scarabBombDigDirectionSnap.ToVector2() * (Main.rand.NextFloat() * 7f + 5f);
					}
				}
			}

			Vector2 vector74 = (angle - (float)Math.PI / 2f).ToRotationVector2();
			for (int num944 = 1; num944 <= 1; num944++)
			{
				for (int num945 = -1; num945 <= 1; num945 += 2)
				{
					for (int num946 = -1; num946 <= 1; num946 += 2)
					{
						var gore14 = Gore.NewGoreDirect(Entity.GetSource_Death(), Projectile.Center - Vector2.One * 20f, Vector2.Zero, Main.rand.Next(61, 64));
						gore14.velocity = gore14.velocity * 0.5f + vector74 * 3f;
						Gore gore2 = gore14;
						gore2.velocity += new Vector2(num945, num946) * 0.2f;
					}
				}
			}

			Projectile.Resize(10, 10);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.Cyan, Color.Orange * .3f, .4f, 200, 20, "SmokeSimple", Vector2.One, Common.Easing.EaseFunction.EaseCircularOut));
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.White, Color.Orange * .3f, .2f, 200, 20, "SmokeSimple", Vector2.One, Common.Easing.EaseFunction.EaseCircularOut));
		}

		if (Projectile.owner == Main.myPlayer)
		{
			var tile = Projectile.Center.ToTileCoordinates();
			var world = tile.ToWorldCoordinates();

			bool wallSplode = false;
			int limit = area * 5; //The length of the tunnel

			if (scarabBombDigDirectionSnap.X == 0 || scarabBombDigDirectionSnap.Y == 0)
				limit = (int)(limit * Math.Sqrt(2.0));

			for (int i = 0; i < limit; i++)
			{
				var vec = world + scarabBombDigDirectionSnap.ToVector2() * 16f * i * 1f;
				var p2 = vec.ToTileCoordinates();
				
				if (Projectile.ShouldWallExplode(p2.ToWorldCoordinates(), 9999, p2.X - 1, p2.X + 1, p2.Y - 1, p2.Y + 1))
				{
					wallSplode = true;
					break;
				}
			}

			for (int i = 0; i < limit; i++)
			{
				var vec2 = world + scarabBombDigDirectionSnap.ToVector2() * 16f * i * 1f;
				var p3 = vec2.ToTileCoordinates();
				int half = area / 2;

				Projectile.ExplodeTiles(p3.ToWorldCoordinates(), 9999, p3.X - half, p3.X + half, p3.Y - half, p3.Y + half, wallSplode);
			}
		}
	}

	private Point ScarabBombSnap()
	{
		var owner = Main.player[Projectile.owner];

		Vector2 vector = Projectile.DirectionTo(owner.Center);
		var result = new Point((!(vector.X > 0f)) ? 1 : (-1), (!(vector.Y > 0f)) ? 1 : (-1));

		if (Math.Abs(vector.X) > Math.Abs(vector.Y) * 2f)
			result.Y = 0;
		else if (Math.Abs(vector.Y) > Math.Abs(vector.X) * 2f)
			result.X = 0;

		return result;
	}

	public override void PostDraw(Color lightColor)
	{
		var texture = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;
		var origin = new Vector2(texture.Width / 2, texture.Height / 2 + 4);
		float scale = Projectile.scale + GetLerp() * .1f;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, scale, default);
	}
}

public class BombDry : Bomb
{
	public override int OriginalType => ProjectileID.DryBomb;

	public override void PostSetDefaults() => area = 6;

	public override void AI()
	{
		base.AI();

		if (Projectile.wet) //Instantly detonate on liquid contact
			Projectile.Kill();
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var pt = Projectile.Center.ToTileCoordinates();
			Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(pt, area, DelegateMethods.SpreadDry);
		}
	}
}

public class BombLava : BombDry
{
	public override int OriginalType => ProjectileID.LavaBomb;

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var pt = Projectile.Center.ToTileCoordinates();
			Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(pt, area, DelegateMethods.SpreadLava);
		}
	}
}

public class BombHoney : BombDry
{
	public override int OriginalType => ProjectileID.HoneyBomb;

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var pt = Projectile.Center.ToTileCoordinates();
			Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(pt, area, DelegateMethods.SpreadHoney);
		}
	}
}

public class BombWet : BombDry
{
	public override int OriginalType => ProjectileID.WetBomb;

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var pt = Projectile.Center.ToTileCoordinates();
			Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(pt, area, DelegateMethods.SpreadWater);
		}
	}
}

public class Dynamite : Bomb
{
	public override int OriginalType => ProjectileID.Dynamite;
	public override LocalizedText DisplayName => Language.GetText("ProjectileName.Dynamite");

	public override void PostSetDefaults()
	{
		SetTimeLeft(60 * 5);
		SetDamage(300, 10);

		area = 30;
	}
}

public class DynamiteSticky : Dynamite
{
	public override int OriginalType => ProjectileID.StickyDynamite;

	public override void PostSetDefaults()
	{
		base.PostSetDefaults();
		sticky = true;
	}
}

public class DynamiteBouncy : Dynamite
{
	public override int OriginalType => ProjectileID.BouncyDynamite;

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Projectile.Bounce(oldVelocity);
		return false;
	}
}