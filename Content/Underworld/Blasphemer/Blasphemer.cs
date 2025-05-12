using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;

namespace SpiritReforged.Content.Underworld.Blasphemer;

[AutoloadGlowmask("255,255,255")]
public class Blasphemer : ClubItem
{
	internal override float DamageScaling => 2.5f;

	public override void SetStaticDefaults() => ItemLootDatabase.AddItemRule(ItemID.ObsidianLockbox, ItemDropRule.Common(Type, 5));
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
	public BlasphemerProj() : base(new Vector2(104)) { }

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

	public override void OnSwingStart() => TrailManager.ManualTrailSpawn(Projectile);

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
		for (int i = 0; i < 16; i++)
		{
			float maxOffset = 40 * TotalScale;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 dustPos = Projectile.Bottom + Vector2.UnitX * offset;
			float velocity = Lerp(4, 1, EaseCircularIn.Ease(Math.Abs(offset) / maxOffset)) * Main.rand.NextFloat(0.25f, 1);
			if (FullCharge)
				velocity *= 1.33f;

			static void ParticleDelegate(Particle p, Vector2 initialVel, float timeOffset, float rotationAmount, float numCycles)
			{
				float progress = EaseQuadOut.Ease(p.Progress);

				p.Velocity = initialVel.RotatedBy(rotationAmount * (float)Math.Sin(TwoPi * (timeOffset + progress) * numCycles)) * EaseQuadOut.Ease(1 - p.Progress);
			}

			ParticleHandler.SpawnParticle(new GlowParticle(dustPos, velocity * -Vector2.UnitY, Color.Yellow, Color.Red, Main.rand.NextFloat(0.3f, 0.6f), Main.rand.Next(30, 60), 3,
				p => ParticleDelegate(p, velocity * -Vector2.UnitY, Main.rand.NextFloat(), Main.rand.NextFloat(PiOver4 / 2), Main.rand.NextFloat(0.5f))));
		}

		if (FullCharge)
		{
			if (Projectile.owner == Main.myPlayer)
				Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, -Vector2.UnitY, ModContent.ProjectileType<Firespike>(),
					(int)(Projectile.damage * DamageScaling * 0.5f), Projectile.knockBack * KnockbackScaling * 0.1f, Projectile.owner);

			for (int i = 0; i < 20; i++)
				Dust.NewDustDirect(Projectile.position - new Vector2(0, 10), Projectile.width, Projectile.height, ModContent.DustType<FireClubDust>(), 0, -Main.rand.NextFloat(5f));

			ParticleHandler.SpawnParticle(new Shatter(position + Vector2.UnitY * 10, Color.OrangeRed * 0.2f, TotalScale, 20));

			SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact.WithVolumeScale(1.5f), position);
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
		Main.EntitySpriteDraw(glow, drawPosition, null, Projectile.GetAlpha(Color.White * GetWindupProgress), Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
	}
}

class Firespike : ModProjectile
{
	public const int TimeLeftMax = 180;

	public override string Texture => "Terraria/Images/Projectile_0";
	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(16);
		Projectile.timeLeft = TimeLeftMax;
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.hide = false;
		Projectile.penetrate = -1;
	}

	public override void AI()
	{
		int surfaceDuration = 0;

		while (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
		{
			Projectile.position.Y--; //Move out of solid tiles

			if (++surfaceDuration > 40)
			{
				Projectile.Kill();
				return;
			}
		}

		surfaceDuration = 0;
		while (!Collision.SolidCollision(Projectile.Center, Projectile.width, Projectile.height, true))
		{
			Projectile.position.Y++;
		}

		if (Main.rand.NextBool(5))
		{
			var pos = Projectile.position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f);

			//ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(4), Color.Black * 0.75f, Main.rand.NextFloat(0.05f, 0.2f), EaseQuadIn, Main.rand.Next(20, 30)));
		}

		if (Main.rand.NextBool(10))
		{
			var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f);
			var velocity = (Vector2.UnitY * -Main.rand.NextFloat(1f, 5f)).RotatedByRandom(0.25f);

			ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.OrangeRed, Main.rand.NextFloat(0.5f), 200, 5));
		}

		if (++Projectile.localAI[0] % 30 == 0)
			SoundEngine.PlaySound(SoundID.Item34 with { PitchRange = (-0.2f, 0.2f), MaxInstances = 3 }, Projectile.Center);
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		const int heightOffset = 100;

		if (new Rectangle(projHitbox.X, projHitbox.Y - heightOffset, projHitbox.Width, projHitbox.Height + heightOffset).Intersects(targetHitbox))
			return true;

		return null;
	}

	public override bool ShouldUpdatePosition() => false;
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 120);
	//Reduce damage with hits
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.FinalDamage *= Math.Max(0.2f, 1f - Projectile.numHits / 8f);

	public override bool PreDraw(ref Color lightColor)
	{
		float timeLeftProgress = Projectile.timeLeft / (float)TimeLeftMax;
		Effect effect = AssetLoader.LoadedShaders["FireStream"];
		effect.Parameters["lightColor"].SetValue(new Color(255, 200, 0).Additive(240).ToVector4());
		effect.Parameters["midColor"].SetValue(new Color(255, 115, 0).Additive(220).ToVector4());
		effect.Parameters["darkColor"].SetValue(new Color(200, 3, 33).Additive(200).ToVector4());

		effect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["distortTexture"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);

		effect.Parameters["textureStretch"].SetValue(new Vector2(2f, 0.5f));
		effect.Parameters["distortStretch"].SetValue(new Vector2(5, 2));

		float globalTimer = Main.GlobalTimeWrappedHourly;
		float scrollSpeed = 1f;
		effect.Parameters["scroll"].SetValue(new Vector2(scrollSpeed * globalTimer));
		effect.Parameters["distortScroll"].SetValue(new Vector2(scrollSpeed * globalTimer) / 2);

		effect.Parameters["intensity"].SetValue(2f * EaseQuadOut.Ease(EaseCircularOut.Ease(timeLeftProgress)));
		effect.Parameters["dissipate"].SetValue(1 - timeLeftProgress);

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Height = 70 * Projectile.scale,
			Length = Lerp(0, 360, EaseCubicOut.Ease(EaseCircularOut.Ease(1 - timeLeftProgress))) * Projectile.scale,
			Position = Projectile.Center - Main.screenPosition - Vector2.UnitY * Lerp(0, 360, EaseCubicOut.Ease(EaseCircularOut.Ease(1 - timeLeftProgress))) / 2,
			Rotation = -PiOver2
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);

		return false;
	}
}