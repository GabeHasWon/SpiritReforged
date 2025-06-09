using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Granite.ShockClub;

class ShockhammerProj : BaseClubProj, IManualTrailProjectile
{
	public static readonly SoundStyle MagicCast = new("SpiritReforged/Assets/SFX/Projectile/MagicCast1")
	{
		Pitch = 0.5f,
		Volume = 1.5f
	};

	public ShockhammerProj() : base(new Vector2(96)) { }

	public override float WindupTimeRatio => 0.9f;

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 90 * MeleeSizeModifier;
		float trailWidth = 40 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = 0;
		float trailLength = 0.4f;

		if (FullCharge)
		{
			trailDist *= 1.1f;
			trailWidth *= 1.1f;
			angleRangeMod = 1.1f;
			rotOffset = -MathHelper.PiOver4 / 2;
			trailLength = 0.6f;
		}

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = Color.LightBlue,
			SecondaryColor = Color.DarkBlue,
			TrailLength = trailLength,
			Intensity = 1,
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));

		parameters.TrailLength *= 1.25f * MathHelper.Lerp(Charge, 1, 0.5f);
		parameters.Color = Color.LightCyan;
		parameters.SecondaryColor = Color.Cyan;
		parameters.UseLightColor = false;
		parameters.Intensity *= 2 * MathHelper.Lerp(Charge, 1, 0.25f);

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, s => SwingTrail.NoiseSwingShaderParams(s, "EnergyTrail", new Vector2(1, 0.2f)), TrailLayer.UnderProjectile));

	}

	public override void OnSwingStart() => TrailManager.ManualTrailSpawn(Projectile);

	public override void OnSmash(Vector2 position)
	{
		TrailManager.TryTrailKill(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		DustClouds(12);

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 200, MathHelper.PiOver2, 0.6f);
		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 240, MathHelper.PiOver2, 0.6f);
		
		if (FullCharge)
		{
			float particleRot = -float.Pi / 2.5f * Projectile.direction;
			if (particleRot < 0)
				particleRot += float.Pi;

			static Color easedCyan(float lerpAmount = 0.5f) => Color.Lerp(Color.LightCyan, Color.Cyan, lerpAmount);
			var particlePos = Vector2.Lerp(Projectile.Center, Owner.Center, 0.25f);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(particlePos, easedCyan(0.33f), 1f, 220, 25, "EnergyTrail", new Vector2(2, 0.5f), EaseFunction.EaseQuarticOut, endRingWidth: 0.3f).WithSkew(0.85f, particleRot));
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(particlePos, easedCyan(0.33f), 1f, 280, 20, "EnergyTrail", new Vector2(2, 0.5f), EaseFunction.EaseQuarticOut, endRingWidth: 0.3f).WithSkew(0.85f + Main.rand.NextFloat(-0.1f, 0.05f), particleRot + Main.rand.NextFloat(-0.1f, 0.1f)));

			//Find a solid tile with an empty tile above it to spawn the projectile on

			Vector2 spawnPos = particlePos;
			Point tilepos = spawnPos.ToTileCoordinates();
			tilepos.Y -= 1;
			int tilesfrombase = 0;
			const int maxtilesfrombase = 3;

			int startX = tilepos.X + (Projectile.direction > 0 ? -1 : 0);

			while (CollisionCheckHelper.CheckSolidTilesAndPlatforms(new Rectangle(startX, tilepos.Y, 1, 1))) //move up until not inside a tile
			{
				tilepos.Y--;

				if (++tilesfrombase >= maxtilesfrombase)
					return;
			}

			while (!CollisionCheckHelper.CheckSolidTilesAndPlatforms(new Rectangle(startX, tilepos.Y + 1, 1, 1))) //move down until just above a tile
			{
				tilepos.Y++;

				if (++tilesfrombase >= maxtilesfrombase)
					return;
			}

			if (Main.myPlayer == Projectile.owner)
			{
				spawnPos = tilepos.ToWorldCoordinates();
				var velocity = Vector2.UnitX * 12 * Owner.direction;

				PreNewProjectile.New(Projectile.GetSource_FromAI("ClubSmash"), spawnPos, velocity, ModContent.ProjectileType<EnergizedShockwave>(), (int)(Projectile.damage * DamageScaling), Projectile.knockBack, Projectile.owner,
					preSpawnAction: delegate (Projectile projectile) { projectile.position.Y -= Projectile.height + 16; });

				SoundEngine.PlaySound(MagicCast, Projectile.Center);
			}
		}
	}

	public override void SafeDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition)
	{
		Texture2D glowTex = GlowmaskItem.ItemIdToGlowmask[ModContent.ItemType<Shockhammer>()].Glowmask.Value;

		for (int i = 0; i < 6; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 6f);
			float opacity = 0.1f * EaseFunction.EaseCircularIn.Ease(Charge) * EaseFunction.EaseSine.Ease(Main.GlobalTimeWrappedHourly % 1);

			Main.EntitySpriteDraw(glowTex, drawPosition + offset, glowTex.Frame(), Color.White.Additive() * opacity, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
		}

		Main.EntitySpriteDraw(glowTex, drawPosition, glowTex.Frame(), Color.White.Additive() * MathHelper.Lerp(Charge, 1, 0.5f) * 0.75f, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.dedServ)
			return;

		var basePosition = Vector2.Lerp(Projectile.Center, target.Center, 0.9f); 
		float particleRot = Projectile.position.DirectionFrom(Projectile.oldPosition).RotatedByRandom(0.3f).ToRotation() + float.Pi / 3 * Projectile.direction;
		static Color easedCyan(float lerpAmount = 0.5f) => Color.Lerp(Color.LightCyan, Color.Cyan, lerpAmount);

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(basePosition, easedCyan(0.4f), 0.8f, 120, 25 + Main.rand.Next(-5, 6), "EnergyTrail", new Vector2(2, 0.5f), EaseFunction.EaseCircularOut, endRingWidth: 0.4f).WithSkew(0.6f, particleRot));

		for(int i = 0; i < 3; i++)
			ParticleHandler.SpawnParticle(new DissipatingImage(basePosition, easedCyan(0.15f), 0, 0.075f, Main.rand.NextFloat(-0.5f, 0.5f), "ElectricScorch", new(0.4f, 0.4f), new(3, 1.5f), 25));

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(basePosition, easedCyan(0.4f), 0.8f, 120, 25 + Main.rand.Next(-5, 6), "EnergyTrail", new Vector2(2, 0.5f), EaseFunction.EaseCircularOut, endRingWidth: 0.4f).WithSkew(0.6f, particleRot + float.Pi / 2));

		for(int i = 0; i < 16; i++)
		{
			Dust.NewDustPerfect(basePosition, DustID.Granite, Main.rand.NextVector2Circular(3, 3), Scale: Main.rand.NextFloat(0.7f, 1.5f)).noGravity = true;
		}
	}
}
