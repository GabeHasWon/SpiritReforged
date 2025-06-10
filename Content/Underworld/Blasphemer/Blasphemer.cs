using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Underworld.Blasphemer;

[AutoloadGlowmask("255,255,255")]
public class Blasphemer : ClubItem
{
	internal override float DamageScaling => 2.5f;

	public override void SetStaticDefaults()
	{
		ItemLootDatabase.AddItemRule(ItemID.ObsidianLockbox, ItemDropRule.Common(Type, 5));
		MoRHelper.AddElement(Item, MoRHelper.Fire, true);
	}

	public override void SafeSetDefaults()
	{
		Item.damage = 38;
		Item.knockBack = 6;
		ChargeTime = 40;
		SwingTime = 30;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 8;
		Item.value = Item.sellPrice(0, 1, 20, 0);
		Item.rare = ItemRarityID.Orange;
		Item.shoot = ModContent.ProjectileType<BlasphemerProj>();
	}
}

[AutoloadGlowmask("255,255,255", false)]
class BlasphemerProj : BaseClubProj, IManualTrailProjectile
{
	public static readonly SoundStyle Impact1 = new("SpiritReforged/Assets/SFX/Item/FieryMaceImpact_1")
	{
		Volume = 0.5f,
		PitchRange = (-0.5f, -0.2f)
	};
	
	public static readonly SoundStyle Impact2 = new("SpiritReforged/Assets/SFX/Item/FieryMaceImpact_2")
	{
		Volume = 0.5f,
		PitchRange = (-0.5f, -0.2f)
	};

	public static readonly SoundStyle Swing1 = new("SpiritReforged/Assets/SFX/Item/FieryMaceSwing_1")
	{
		PitchVariance = 0.2f,
		PitchRange = (-0.4f, 0f)
	};

	public static readonly SoundStyle Swing2 = new("SpiritReforged/Assets/SFX/Item/FieryMaceSwing_2")
	{
		PitchVariance = 0.2f,
		PitchRange = (-0.4f, 0f)
	};

	public BlasphemerProj() : base(new Vector2(100)) { }

	public override float WindupTimeRatio => 0.8f;
	public override float SwingShrinkThreshold => 0.65f;

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 78 * MeleeSizeModifier;
		float trailWidth = 60 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = -PiOver4 / 4;
		float trailLength = 0.75f;

		if (FullCharge)
		{
			trailDist *= 1.2f;
			trailWidth *= 1.2f;
			angleRangeMod = 1.2f;
			trailLength = 1;
		}

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist * 0.95f, trailWidth * 0.9f)
		{
			Color = Color.DarkGray,
			SecondaryColor = Color.Black,
			TrailLength = 0.45f,
			Intensity = 1.5f,
			UseLightColor = true
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));

		SwingTrailParameters flameTrailParameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = Color.Yellow.Additive(200),
			SecondaryColor = Color.Red.Additive(150),
			TrailLength = trailLength,
			Intensity = 1.75f,
			UseLightColor = false
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, flameTrailParameters, GetSwingProgressStatic, s => SwingTrail.FireSwingShaderParams(s, new Vector2(4, 0.4f) / 1.5f), TrailLayer.UnderProjectile));

		flameTrailParameters.Distance /= 2;
		flameTrailParameters.Width *= 0.75f;
		flameTrailParameters.TrailLength *= 0.5f;
		tM.CreateCustomTrail(new SwingTrail(Projectile, flameTrailParameters, GetSwingProgressStatic, s => SwingTrail.FireSwingShaderParams(s, new Vector2(2, 0.4f) / 1.5f), TrailLayer.UnderProjectile));
	}

	public override void SafeSetDefaults() => _parameters.ChargeColor = Color.OrangeRed;

	public override void OnSwingStart()
	{
		TrailManager.ManualTrailSpawn(Projectile);

		if (FullCharge)
		{
			SoundEngine.PlaySound(Main.rand.Next([Swing1, Swing2]), Projectile.Center);
		}
	}

	public override void OnSmash(Vector2 position)
	{
		TrailManager.TryTrailKill(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 280, PiOver2, 0.4f);

		//black smoke particles
		for (int i = 0; i < 16; i++)
		{
			Vector2 smokePos = Projectile.Bottom + Vector2.UnitX * Main.rand.NextFloat(-10, 10);

			float easedProgress = EaseQuadOut.Ease(i / 16f);
			float scale = Lerp(0.12f, 0.03f, easedProgress) * TotalScale;

			float speed = Lerp(0.5f, 3.5f, easedProgress);
			int lifeTime = (int)(Lerp(30, 40, easedProgress) + Main.rand.Next(-5, 6));

			ParticleHandler.SpawnParticle(new SmokeCloud(smokePos, -Vector2.UnitY * speed, Color.Black * 0.66f, scale, EaseCircularIn, lifeTime));
		}

		//Sine movement ember particles
		for (int i = 0; i < 12; i++)
		{
			float maxOffset = 40 * TotalScale;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 dustPos = Projectile.Bottom + Vector2.UnitX * offset;
			float velocity = Lerp(4, 1, EaseCircularIn.Ease(Math.Abs(offset) / maxOffset)) * Main.rand.NextFloat(0.25f, 1);
			if (FullCharge)
				velocity *= 1.33f;

			static void ParticleDelegate(Particle p, Vector2 initialVel, float timeOffset, float rotationAmount, float numCycles)
			{
				float sineProgress = EaseQuadOut.Ease(p.Progress);

				p.Velocity = initialVel.RotatedBy(rotationAmount * (float)Math.Sin(TwoPi * (timeOffset + sineProgress) * numCycles)) * (1 - p.Progress);
			}

			float timeOffset = Main.rand.NextFloat();
			float rotationAmount = Main.rand.NextFloat(PiOver4);
			float numCycles = Main.rand.NextFloat(0.5f, 2);

			ParticleHandler.SpawnParticle(new GlowParticle(dustPos, velocity * -Vector2.UnitY, Color.Yellow, Color.Red, Main.rand.NextFloat(0.3f, 0.6f), Main.rand.Next(30, 80), 3,
				p => ParticleDelegate(p, velocity * -Vector2.UnitY, timeOffset, rotationAmount, numCycles)));
		}

		if (FullCharge)
		{
			if (Projectile.owner == Main.myPlayer)
			{
				const int numPillars = 5;

				Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.UnitX * Projectile.direction, ModContent.ProjectileType<Firespike>(),
					(int)(Projectile.damage * DamageScaling * 0.5f), Projectile.knockBack * KnockbackScaling * 0.1f, Projectile.owner, numPillars - 1);
			}

			for (int i = 0; i < 20; i++)
				Dust.NewDustDirect(Projectile.position - new Vector2(0, 10), Projectile.width, Projectile.height, ModContent.DustType<FireClubDust>(), 0, -Main.rand.NextFloat(5f));

			ParticleHandler.SpawnParticle(new Shatter(position + Vector2.UnitY * 10, Color.OrangeRed * 0.3f, TotalScale, 30));
			SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact.WithVolumeScale(1.5f), position);

			SoundEngine.PlaySound(Main.rand.Next([Impact1, Impact2]), position);
		}
	}

	public override void SafeAI()
	{
		if (FullCharge && CheckAIState(AIStates.SWINGING))
		{
			int count = Main.rand.Next(1, 4);
			for (int i = 0; i < count; i++)
			{
				var center = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(30f);
				var velocity = (Projectile.velocity * Main.rand.NextFloat(3f)).RotatedBy(Projectile.rotation);

				ParticleHandler.SpawnParticle(new EmberParticle(center, velocity / 2, Color.Yellow, Color.Red, Main.rand.NextFloat(0.3f), 60, 5));
			}
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 300);

	public override void SafeDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition)
	{
		var glow = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;
		float ease = EaseCubicIn.Ease(GetWindupProgress);

		Main.EntitySpriteDraw(glow, drawPosition, null, Projectile.GetAlpha(Color.White * ease), Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
		if ((AIStates)AiState is not AIStates.POST_SMASH)
			Main.EntitySpriteDraw(glow, drawPosition, null, Projectile.GetAlpha(Color.White.Additive() * ease), Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
	}
}