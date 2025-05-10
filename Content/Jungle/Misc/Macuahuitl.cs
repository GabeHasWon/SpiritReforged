using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.BuffCommon.Stacking;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.ModCompat;
using static SpiritReforged.Common.Easing.EaseFunction;
using Terraria.Audio;

namespace SpiritReforged.Content.Jungle.Misc;

public class Macuahuitl : ClubItem
{
	internal override float DamageScaling => 4f;
	internal override float KnockbackScaling => 5f;

	public override void SetStaticDefaults()
	{
		if (!CrossMod.Classic.Enabled)
			NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.WitchDoctor, new NPCShop.Entry(Type)));
	}

	public override void SafeSetDefaults()
	{
		Item.damage = 16;
		Item.knockBack = 2;
		ChargeTime = 40;
		SwingTime = 24;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.shoot = ModContent.ProjectileType<MacuahuitlProj>();
	}
}

class MacuahuitlProj : BaseClubProj, IManualTrailProjectile
{
	public MacuahuitlProj() : base(new Vector2(82)) { }

	public bool ChargeStrike => FullCharge && CheckAIState(AIStates.SWINGING);
	public override float WindupTimeRatio => 0.8f;

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 76 * MeleeSizeModifier;
		float trailWidth = 30 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = 0;

		if (FullCharge)
		{
			trailDist *= 1.1f;
			trailWidth *= 1.1f;
			angleRangeMod = 1.2f;
			rotOffset = -MathHelper.PiOver4 / 2;
		}

		if (CheckAIState(AIStates.SWINGING)) //The trail created when OnSwingStart is called
		{
			SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
			{
				Color = Color.White,
				SecondaryColor = Color.LightGray,
				TrailLength = 0.33f,
				Intensity = 0.5f,
			};

			tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
		}
		else //The trail created when ChargeComplete is called
		{
			SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist - 10, trailWidth + 20)
			{
				Color = Color.White,
				SecondaryColor = Color.LightGray,
				TrailLength = 0.5f,
				Intensity = 0.5f,
				DissolveThreshold = 1f
			};

			tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, s => SwingTrail.NoiseSwingShaderParams(s, "vnoise", new Vector2(1f)), TrailLayer.UnderProjectile));
		}
	}

	private new static float GetSwingProgressStatic(Projectile Proj)
	{
		if (Proj.ModProjectile is BaseClubProj baseClub)
			return baseClub.CheckAIState(AIStates.SWINGING) ? EaseQuadOut.Ease(baseClub.GetSwingProgress) : (baseClub.BaseRotation + 0.8f) / MathHelper.TwoPi % 1;

		return 0;
	}

	public override void SafeSetDefaults()
	{
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 20;
	}

	internal override void ChargeComplete(Player owner) => TrailManager.ManualTrailSpawn(Projectile);
	public override void OnSwingStart()
	{
		TrailManager.ManualTrailSpawn(Projectile);
		Projectile.ResetLocalNPCHitImmunity();
	}

	internal override float ChargedRotationInterpolate(float progress)
	{
		if (FullCharge && ++Projectile.localAI[0] % 28f == 0)
			SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);

		float rate = 0.22f * progress;
		return BaseRotation + rate;
	}

	public override void OnSmash(Vector2 position)
	{
		TrailManager.TryTrailKill(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		if (FullCharge)
		{
			float angle = MathHelper.PiOver4 * 1.5f;
			if (Projectile.direction > 0)
				angle = -angle + MathHelper.Pi;

			DoShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.5f), 280, angle, 0.4f);

			var velocity = Vector2.UnitX * Projectile.direction * 8;
			var center = Projectile.Center - Vector2.UnitY * 8;

			int damage = (int)(Projectile.damage * DamageScaling) / 2;
			float knockback = Projectile.knockBack * KnockbackScaling / 2f;

			Projectile.NewProjectile(Projectile.GetSource_FromAI(), center, velocity, ModContent.ProjectileType<Shockwave>(), damage, knockback, Projectile.owner);
		}

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 180, MathHelper.PiOver2, 0.4f);
	}

	public override bool? CanDamage() => CheckAIState(AIStates.POST_SMASH) ? false : null;
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (ChargeStrike)
		{
			modifiers.FinalDamage *= DamageScaling;
			modifiers.Knockback *= KnockbackScaling;

			if (target.HasStackingBuff<StackingBleed>(out var buff))
				modifiers.FinalDamage += buff.stacks * buff.duration * 0.001f; //Detonate stacks
		}
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		if (ChargeStrike)
		{
			modifiers.FinalDamage *= DamageScaling;
			modifiers.Knockback *= KnockbackScaling;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		var basePosition = Vector2.Lerp(Projectile.Center, target.Center, 0.6f);
		Vector2 directionUnit = basePosition.DirectionFrom(Owner.MountedCenter) * TotalScale;

		int numParticles = ChargeStrike ? 12 : 8;
		for (int i = 0; i < numParticles; i++)
		{
			float maxOffset = 15;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 position = basePosition + directionUnit.RotatedBy(MathHelper.PiOver2) * offset;
			float velocity = MathHelper.Lerp(12, 2, Math.Abs(offset) / maxOffset) * Main.rand.NextFloat(0.9f, 1.1f);

			if (ChargeStrike)
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

		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 3, Color.LightGray, 0.06f * TotalScale, EaseCubicOut, 30));
		ParticleHandler.SpawnParticle(new SmokeCloud(basePosition, directionUnit * 6, Color.LightGray, 0.08f * TotalScale, EaseCubicOut, 30));

		if (ChargeStrike)
		{
			if (!target.RemoveStackingBuff<StackingBleed>())
				return;

			float dirUnit = target.AngleFrom(Owner.Center);

			ParticleHandler.SpawnParticle(new DissipatingImage(basePosition, Color.DarkRed, 0, 0.3f, Main.rand.NextFloat(-0.5f, 0.5f), "Fire", new(0.4f, 0.4f), new(3, 1.5f), 25) { UseLightColor = true });
			ParticleHandler.SpawnParticle(new DissipatingImage(basePosition, Color.Red, 0, 0.15f, Main.rand.NextFloat(-0.5f, 0.5f), "Fire", new(0.4f, 0.4f), new(3, 1.5f), 25) { UseLightColor = true });

			for (int i = 0; i < 3; i++)
			{
				float newRotation = dirUnit + Main.rand.NextFloat(-1.0f, 1.0f);
				var color = Color.Lerp(Color.DarkRed, Color.Red, Main.rand.NextFloat());
				var p = new TexturedPulseCircle(basePosition, color, Color.Black, Main.rand.NextFloat(1f, 3f), Main.rand.Next(100, 180), Main.rand.Next(10, 30), "SmokeSimple", new Vector2(2f, 1f), EaseCircularOut);

				p.Velocity = (Vector2.UnitX * 3).RotatedBy(newRotation);
				p.UseLightColor = true;
				p.WithSkew(Main.rand.NextFloat(0.8f, 0.9f), newRotation + MathHelper.PiOver2);

				ParticleHandler.SpawnParticle(p);
			}
		}
		else
		{
			target.AddStackingBuff<StackingBleed>(300, 1);
		}
	}
}

public class StackingBleed : StackingBuff
{
	public override void OnAdded() => MaxStacks = 10;
	public override void UpdateEffects(NPC npc)
	{
		npc.lifeRegen = Math.Min(npc.lifeRegen, 0) - 4 * stacks;
		
		if (Main.rand.NextFloat() < stacks / (float)MaxStacks)
		{
			var d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood);
			d.noGravity = true;
		}
	}
}