using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
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
		Item.value = Item.sellPrice(0, 2, 50, 0);
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
		Volume = 0.6f,
		PitchVariance = 0.2f,
		PitchRange = (-0.4f, 0f)
	};

	public static readonly SoundStyle Swing2 = new("SpiritReforged/Assets/SFX/Item/FieryMaceSwing_2")
	{
		Volume = 0.6f,
		PitchVariance = 0.2f,
		PitchRange = (-0.4f, 0f)
	};

	public BlasphemerProj() : base(new Vector2(100)) { }

	public override float WindupTimeRatio => 0.8f;
	public override float SwingShrinkThreshold => 0.65f;

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 90 * MeleeSizeModifier;
		float trailWidth = 60 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = -PiOver4 / 4;

		if (FullCharge)
		{
			trailWidth *= 1.1f;
			angleRangeMod = 1.2f;
		}

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist * 0.95f, trailWidth * 0.9f)
		{
			Color = Color.DarkGray,
			SecondaryColor = Color.Black,
			TrailLength = FullCharge ? 0.45f : 0.35f,
			Intensity = 1.5f,
			UseLightColor = true,
			DissolveThreshold = 0.95f
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
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

		//black smoke particles
		for (int i = 0; i < 16; i++)
		{
			Vector2 smokePos = Projectile.Bottom + Vector2.UnitX * Main.rand.NextFloat(-10, 10);

			float easedProgress = EaseQuadOut.Ease(i / 16f);
			float scale = Lerp(0.2f, 0.1f, easedProgress);

			float speed = Lerp(0.5f, 3f, easedProgress);
			int lifeTime = (int)(Lerp(30, 50, easedProgress) + Main.rand.Next(-5, 6));

			var smokeCloud = new SmokeCloud(smokePos, -Vector2.UnitY * speed, Color.Gray, scale, EaseQuadIn, lifeTime)
			{
				SecondaryColor = Color.DarkSlateGray,
				TertiaryColor = Color.Black,
				ColorLerpExponent = 2,
				Intensity = 0.33f,
				Layer = ParticleLayer.BelowProjectile
			};
			ParticleHandler.SpawnParticle(smokeCloud);
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

			SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact.WithVolumeScale(1.5f), position);

			SoundEngine.PlaySound(Main.rand.Next([Impact1, Impact2]), position);
		}

		else if (Projectile.owner == Main.myPlayer)
			Firespike.EruptFX(Projectile.Bottom, 1);
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

		if(CheckAIState(AIStates.SWINGING))
		{
			for(int i = 0; i < (FullCharge ? 3 : 2); i++)
			{
				Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(10, 10);
				Vector2 velocity = -Vector2.UnitY + Main.rand.NextVector2Unit() + Owner.velocity / 2;
				Color[] colors = [new Color(255, 200, 0, 100), new Color(255, 115, 0, 100), new Color(200, 3, 33, 100)];
				float scale = Main.rand.NextFloat(0.09f, 0.15f) * TotalScale * (FullCharge ? 1.25f : 1);
				int maxTime = (int)(Main.rand.Next(10, 35) / (FullCharge ? 1 : 1.33f));

				ParticleHandler.SpawnParticle(new FireParticle(position, velocity, colors, 1.25f, scale, EaseQuadIn, maxTime) 
				{ 
					ColorLerpExponent = 2.5f 
				});
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
		{
			for(int i = 0; i < 6; i++)
			{
				Vector2 offset = Vector2.UnitX.RotatedBy(TwoPi * i / 6f) * 2;

				Main.EntitySpriteDraw(glow, drawPosition + offset, null, Projectile.GetAlpha(Color.White.Additive() * ease) * 0.25f, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
			}

			Main.EntitySpriteDraw(glow, drawPosition, null, Projectile.GetAlpha(Color.White.Additive() * ease), Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
		}
	}
}