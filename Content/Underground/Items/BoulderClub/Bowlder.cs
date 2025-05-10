using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.MathHelpers;
using System.IO;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Underground.Items.BoulderClub;

public class Bowlder : ClubItem
{
	internal override float DamageScaling => 1.3f;

	public override void SafeSetDefaults()
	{
		Item.damage = 45;
		Item.knockBack = 8;
		ChargeTime = 45;
		SwingTime = 35;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 70, 0);
		Item.rare = ItemRarityID.Blue;
		Item.shoot = ModContent.ProjectileType<BowlderProj>();
	}
}

class BowlderProj : BaseClubProj, IManualTrailProjectile
{
	public const float SHOOT_SPEED = 8;

	public BowlderProj() : base(new Vector2(72)) { }

	public Vector2 StoredShotTrajectory;
	public Vector2 StoredTargetPos;

	public override float WindupTimeRatio => 0.6f;

	public override string Texture => (GetType().Namespace + '.' + Name).Replace('.', '/');
	public override void SafeSetStaticDefaults() => Main.projFrames[Type] = 2;

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 60 * MeleeSizeModifier;
		float trailWidth = 40 * MeleeSizeModifier;

		if (!FullCharge)
		{
			SwingTrailParameters parameters = new(AngleRange, -HoldAngle_Final, trailDist, trailWidth)
			{
				Color = Color.White,
				SecondaryColor = Color.LightGray,
				TrailLength = 0.33f,
				Intensity = 0.5f,
			};

			tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
		}
	}

	public override void OnSwingStart()
	{
		TrailManager.ManualTrailSpawn(Projectile);
		if(FullCharge && Main.myPlayer == Owner.whoAmI)
		{
			StoredShotTrajectory = Owner.GetArcVel(Main.MouseWorld, 0.5f, SHOOT_SPEED);
			StoredTargetPos = Main.MouseWorld - Owner.MountedCenter;

			Projectile.netUpdate = true;
		}
	}

	public override void Swinging(Player owner)
	{
		base.Swinging(owner);

		float launchAngle = StoredShotTrajectory.ToRotation();
		launchAngle += MathHelper.PiOver2;
		float launchThreshold = MathHelper.Lerp(0.22f, 0.28f, launchAngle / MathHelper.Pi);

		if (FullCharge && GetSwingProgress >= launchThreshold && Projectile.frame == 0)
		{
			if (Main.myPlayer == Projectile.owner)
			{
				var adjustedTrajectory = ArcVelocityHelper.GetArcVel(Projectile.Center - Owner.MountedCenter, StoredTargetPos, 0.5f, StoredShotTrajectory.Length());

				//Prevent backwards or no movement if aiming straight down with a slightly less accurate calc
				bool backwardsMovement = Projectile.direction > 0 && adjustedTrajectory.X < 0 || Projectile.direction < 0 && adjustedTrajectory.X > 0;
				if (backwardsMovement)
					adjustedTrajectory = StoredShotTrajectory;

				adjustedTrajectory += owner.velocity / 3;

				//Prevent spawning inside or through tiles
				Vector2 spawnPos = GetHeadPosition(16);

				bool spawnInTile = Collision.SolidTiles(spawnPos - new Vector2(16) * MeleeSizeModifier, (int)(32 * MeleeSizeModifier), (int)(32 * MeleeSizeModifier), true);
				bool ownerLineCheck = CollisionCheckHelper.LineOfSightSolidTop(spawnPos, owner.MountedCenter);

				while ((spawnInTile || ownerLineCheck) && spawnPos.Y > owner.MountedCenter.Y)
					spawnPos.Y--;

				PreNewProjectile.New(Projectile.GetSource_FromAI(), spawnPos, adjustedTrajectory, ModContent.ProjectileType<RollingBowlder>(), (int)(Projectile.damage * DamageScaling), Projectile.knockBack, Projectile.owner, Owner.direction, preSpawnAction: delegate (Projectile p)
				{
					p.Size *= MeleeSizeModifier;
					p.scale = MeleeSizeModifier;
				});

				if (!Main.dedServ)
				{
					for (int i = 1; i < 7; i++)
					{
						int type = Mod.Find<ModGore>("BowlderRope" + i).Type;
						Gore.NewGore(Projectile.GetSource_Death(), Projectile.position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), adjustedTrajectory * 0.1f, type, MeleeSizeModifier);
					}

					for (int i = 0; i < 8; i++)
					{
						var random = (adjustedTrajectory * Main.rand.NextFloat()).RotatedByRandom(1) / 3;
						Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Rope, random.X, random.Y, Scale: Main.rand.NextFloat());
					}
				}
			}

			Projectile.frame = 1;
		}
	}

	internal override float SwingingRotationInterpolate(float progress)
	{
		float baseRot = base.SwingingRotationInterpolate(progress);
		if (FullCharge)
		{
			var temp = new Vector2(Math.Abs(StoredShotTrajectory.X), StoredShotTrajectory.Y);
			float baseOffset = temp.ToRotation();
			float offset = baseOffset;
			int offsetSign = Math.Sign(offset);

			//Slightly constrain the starting offset to reduce big jumps in position
			if (Math.Abs(offset) > 0.25f)
				offset = MathHelper.Lerp(offset, 0.25f * offsetSign, 0.5f);

			if (Math.Abs(offset) > 0.75f)
				offset = MathHelper.Lerp(offset, 0.25f * offsetSign, 0.75f);

			//Then gradually restore it
			offset = MathHelper.Lerp(offset, temp.ToRotation(), EaseFunction.EaseQuadOut.Ease(GetSwingProgress));

			baseRot += offset;
		}

		return baseRot;
	}

	internal override bool CanCollide(float progress) => !FullCharge;
	public override void OnSmash(Vector2 position)
	{
		TrailManager.TryTrailKill(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		DustClouds(8);

		if (FullCharge)
		{
			float angle = MathHelper.PiOver4 * 1.5f;
			if (Projectile.direction > 0)
				angle = -angle + MathHelper.Pi;

			DoShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.5f), 280, angle, 0.4f);
		}

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 180, MathHelper.PiOver2, 0.4f);
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

			if (!Main.rand.NextBool(3))
				Dust.NewDustPerfect(position, DustID.t_LivingWood, particleVel / 3, Scale: 0.5f);
		}

		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 3, Color.LightGray, 0.06f * TotalScale, EaseFunction.EaseCubicOut, 30));
		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 6, Color.LightGray, 0.08f * TotalScale, EaseFunction.EaseCubicOut, 30));
	}

	internal override void SendExtraDataSafe(BinaryWriter writer)
	{
		writer.WritePackedVector2(StoredShotTrajectory);
		writer.WritePackedVector2(StoredTargetPos);
	}

	internal override void ReceiveExtraDataSafe(BinaryReader reader)
	{
		StoredShotTrajectory = reader.ReadPackedVector2();
		StoredTargetPos = reader.ReadPackedVector2();
	}
}