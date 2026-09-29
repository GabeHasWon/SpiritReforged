using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest.PumpkinClub;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.ShovelClub;
class ShovelClubProj : BaseClubProj
{
	public ShovelClubProj() : base(new Vector2(76, 76)) { }

	public override float WindupTimeRatio => 0.8f;
	public override float HoldAngle_Intial => -MathHelper.PiOver4;
	public override float HoldAngle_Final => -MathHelper.PiOver2;
	public override float SwingAngle_Max => -MathHelper.Pi * 1.45f;
	public override float SwingPhaseThreshold => 0.15f;

	bool DugTile = false;

	int pauseTimer;

	public override void OnSwingStart()
	{
		if (!Main.dedServ)
			CreateTrail(TrailSystem.ProjectileRenderer);
	}

	public void CreateTrail(ProjectileTrailRenderer renderer)
	{
		float trailDist = 65 * MeleeSizeModifier;
		float trailWidth = 70 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = -1;
		
		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = Color.White,
			SecondaryColor = Color.Gray,
			TrailLength = 1f,
			Intensity = 0.5f,
		};

		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
	}

	public override void Swinging(Player owner)
	{
		float swingProgress = GetSwingProgress;

		bool validTile = CollisionChecks.Tiles(Projectile.Hitbox, CollisionChecks.AnySurface);
		BaseScale = 1;

		if (pauseTimer <= 0)
			_swingTimer++;
		else
		{
			pauseTimer--;
			if (pauseTimer == 0)
				Dig(owner);
		}

		BaseRotation = SwingingRotationInterpolate(swingProgress);

		if (validTile && CanCollide(swingProgress) && !DugTile)
		{
			SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
			pauseTimer = 9;
			DugTile = true;
		}

		if (swingProgress >= SwingShrinkThreshold)
		{
			float shrinkProgress = (swingProgress - SwingShrinkThreshold) / (1 - SwingShrinkThreshold);
			shrinkProgress = MathHelper.Clamp(shrinkProgress, 0, 1);

			BaseScale = MathHelper.Lerp(1, 0, EaseBuilder.EaseCubicIn.Ease(shrinkProgress));

			if (swingProgress > 1)
				Projectile.Kill();
		}
	}

	private void Dig(Player owner)
	{
		float strength = FullCharge ? 1f : 0.66f;

		if (Main.myPlayer == owner.whoAmI)
		{
			var direction = Vector2.Normalize(Projectile.oldPosition - Projectile.position);
			ScreenshakeHelper.Shake(owner.Center, direction, 1 + Charge * 2, 6, (int)(10 * (0.5f + Charge / 2)));
		}

		DustClouds(4);

		Collision.HitTiles(Projectile.Center + Main.rand.NextVector2Circular(25, 25), Vector2.Zero, 16, 16);

		if (FullCharge)
		{
			for (int i = 0; i < 5; i++)
			{
				Vector2 particleVel = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(4f, 9f) * strength - Vector2.UnitY * 5f * strength;
				Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(25, 25) + Vector2.UnitY * 20;

				bool big = Main.rand.NextBool(4);

				ParticleHandler.SpawnParticle(new TileChunkParticle(pos.ToTileCoordinates(), pos, particleVel, Main.rand.Next(30, 70), big, !big && Main.rand.NextBool()));
			}

			for (int i = 0; i < 4; i++)
			{
				Vector2 velocity = Vector2.UnitX * Projectile.direction;
				if (Projectile.owner == Main.myPlayer)
					velocity = Projectile.Center.DirectionTo(Main.MouseWorld).RotatedByRandom(0.2f) * Main.rand.NextFloat(8f, 12f) - Vector2.UnitY * 2f;

				Projectile.NewProjectile(Projectile.GetSource_FromThis("SpiritReforged: Shovel Club Dig"), Projectile.Center, velocity, ModContent.ProjectileType<ShovelClubBoneProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
			}				
		}
		
		for (int i = 0; i < (FullCharge ? 15 : 10); i++)
		{
			Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(25, 25);
			Point tilePosition = dustPosition.ToTileCoordinates();
			int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

			Dust dust = Main.dust[dustIndex];
			dust.velocity = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(3f, 7f) * strength - Vector2.UnitY * 5f * strength;
			dust.noGravity = !Main.rand.NextBool(5);
			dust.noLightEmittence = true;
			dust.scale = Main.rand.NextFloat(0.7f, 1.2f);

			Vector2 particlePos = Projectile.Center + Main.rand.NextVector2Circular(25, 25);

			Vector2 particleVel = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(3f, 7f) * strength - Vector2.UnitY * 5f * strength;
			particleVel *= Main.rand.NextFloat(2f) * strength;
			Color[] colors = GetTilePalette(particlePos);
			
			if (FullCharge)
				Dust.NewDustPerfect(particlePos, DustID.Bone, particleVel * 0.65f, 200, default, Main.rand.NextFloat(2f));

			ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, colors[0], Main.rand.NextFloat(0.06f, 0.1f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
			{
				Pixellate = true,
				DissolveAmount = 1,
				Intensity = 0.9f,
				SecondaryColor = colors[1],
				TertiaryColor = colors[2],
				PixelDivisor = 3,
				Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
				ColorLerpExponent = 0.5f,
				Layer = ParticleLayer.BelowSolid
			});
		}

		for (int i = 0; i < (FullCharge ? 19 : 11); i++)
		{
			Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(25, 25);
			Point tilePosition = dustPosition.ToTileCoordinates();
			int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

			Dust dust = Main.dust[dustIndex];
			dust.velocity = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(3f, 7f) * strength - Vector2.UnitY * 5f * strength;
			dust.noGravity = Main.rand.NextBool(5);
			dust.noLightEmittence = true;
			dust.scale = Main.rand.NextFloat(0.5f, 2f);
			dust.fadeIn = 1f;
			dust.alpha = 200;

			Vector2 particlePos = Projectile.Center + Main.rand.NextVector2Circular(25, 25);

			Vector2 particleVel = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(4f, 9f) * strength - Vector2.UnitY * 2f * strength;
			particleVel *= Main.rand.NextFloat(2f) * strength;
			Color[] colors = GetTilePalette(particlePos);

			if (FullCharge)
				Dust.NewDustPerfect(particlePos, DustID.Bone, particleVel * 1.5f, 150, default, Main.rand.NextFloat(3f)).noGravity = true;

			ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, colors[0] * 0.2f, Main.rand.NextFloat(0.1f, 0.2f), EaseFunction.EaseCircularOut, Main.rand.Next(50, 90))
			{
				Pixellate = true,
				DissolveAmount = 1,
				Intensity = 0.9f,
				SecondaryColor = colors[1],
				TertiaryColor = colors[2],
				PixelDivisor = 3,
				Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
				ColorLerpExponent = 0.5f,
				Layer = ParticleLayer.BelowSolid
			});

			particleVel = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(20f, 40f) * strength - Vector2.UnitY * 5f * strength;
			var p = new ImpactLine(Projectile.Center, particleVel, Color.White * 0.5f, new Vector2(0.15f, 0.6f) * TotalScale, Main.rand.Next(20, 30), 0.8f);
			p.UseLightColor = true;
			ParticleHandler.SpawnParticle(p);

			particleVel = Vector2.UnitX.RotatedByRandom(0.5f) * Projectile.direction * Main.rand.NextFloat(20f, 40f) * strength - Vector2.UnitY * 5f * strength;
			p = new ImpactLine(Projectile.Center, particleVel, colors[1] * 0.5f, new Vector2(0.15f, 0.6f) * TotalScale, Main.rand.Next(20, 30), 0.8f);
			p.UseLightColor = true;
			ParticleHandler.SpawnParticle(p);
		}

		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, owner.Center);
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, owner.Center);
			SoundEngine.PlaySound(DefaultSmash with { Volume = 0.5f, PitchVariance = 0.2f }, owner.Center);
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		var basePosition = Vector2.Lerp(Projectile.Center, target.Center, 0.6f);
		Vector2 directionUnit = basePosition.DirectionFrom(Owner.MountedCenter) * TotalScale;

		int numParticles = FullCharge ? 12 : 8;
		for (int i = 0; i < numParticles; i++)
		{
			float maxOffset = 15;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 position = basePosition + directionUnit.RotatedBy(MathHelper.PiOver2) * offset;
			float velocity = MathHelper.Lerp(12, 2, Math.Abs(offset) / maxOffset) * Main.rand.NextFloat(0.9f, 1.1f);
			if (FullCharge)
				velocity *= 1.5f;

			float rotationOffset = MathHelper.PiOver4 * offset / maxOffset;
			rotationOffset *= Main.rand.NextFloat(0.9f, 1.1f);

			Vector2 particleVel = directionUnit.RotatedBy(rotationOffset) * velocity;
			var p = new ImpactLine(position, particleVel, Color.White * 0.5f, new Vector2(0.15f, 0.6f) * TotalScale, Main.rand.Next(15, 20), 0.8f);
			p.UseLightColor = true;
			ParticleHandler.SpawnParticle(p);
		}

		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 3, Color.LightGray, 0.06f * TotalScale, EaseFunction.EaseCubicOut, 30));
		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 6, Color.LightGray, 0.08f * TotalScale, EaseFunction.EaseCubicOut, 30));
	}

	static Color[] GetTilePalette(Vector2 input)
	{
		Point tilePosition = input.ToTileCoordinates();
		Tile tile = Framing.GetTileSafely(tilePosition);	

		if (!tile.HasTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType] || tile.TileType == TileID.Dirt)
			return [new Color(151, 107, 75) * 0.7f, new Color(114, 81, 56) * 0.7f, new Color(30, 19, 12) * 0.7f];

		var material = TileMaterial.FindMaterial(tile.TileType);
		return [material.Color, (material.Color * 0.8f).Additive(255) * 1.33f, (material.Color * 0.25f).Additive(255) * 0.5f];
	}
}

class ShovelClubBoneProjectile : ModProjectile
{
	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 0;

		Main.projFrames[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.timeLeft = 240;
		Projectile.tileCollide = false;
		Projectile.Size = new(16);
		Projectile.penetrate = 1;

		Projectile.frame = Main.rand.Next(2);
	}

	public override void AI()
	{
		Projectile.rotation += Projectile.velocity.Length() * 0.01f;

		if (++Projectile.ai[0] > 20)
		{
			if (!Projectile.tileCollide)
				Projectile.tileCollide = true;

			Projectile.velocity.X *= 0.99f;
			Projectile.velocity.Y += 0.2f;
			if (Projectile.velocity.Y > 0)
				Projectile.velocity.Y *= 1.06f;

			if (Projectile.velocity.Y > 16)
				Projectile.velocity.Y = 16;
		}

		if (Main.rand.NextBool(4))
			Dust.NewDustPerfect(Projectile.Center, DustID.Bone, Main.rand.NextVector2Circular(2, 2), 150, default, Main.rand.NextFloat(1.5f)).noGravity = true;
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.NPCHit2, Projectile.Center);

		for (int i = 0; i < 5; i++)
		{
			Vector2 particleVel = Main.rand.NextVector2CircularEdge(5, 5);
			var p = new ImpactLine(Projectile.Center, particleVel, Color.White * 0.65f, new Vector2(0.15f, 0.6f) * 0.66f, Main.rand.Next(20, 30), 0.85f);
			p.UseLightColor = true;
			ParticleHandler.SpawnParticle(p);

			Dust.NewDustPerfect(Projectile.Center, DustID.Bone, Main.rand.NextVector2Circular(5, 5), 50, default, Main.rand.NextFloat(1f, 2f)).noGravity = true;
		}

		float strength = Main.rand.NextFloat(0.9f, 1.1f);

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, new(91, 91, 61, 50), new(36, 36, 24), 1f, 50 * strength, (int)(30 * strength), "Smoke", Vector2.One, EaseFunction.EaseQuinticOut)
		{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

	}

	public override bool PreDraw(ref Color lightColor)
	{
		var tex = TextureAssets.Projectile[Type].Value;

		var frame = tex.Frame(1, 2, 0, Projectile.frame);

		for (int i = 0; i < Projectile.oldPos.Length; i++)
		{
			float lerp = 1f - i / (float)Projectile.oldPos.Length;
			Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;

			Main.spriteBatch.Draw(tex, pos, frame, lightColor * lerp, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);

		return false;
	}
}
