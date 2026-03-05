using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
// TODO: Modernize trail rendering
public class AdornedArrow : ModProjectile//, ITrailProjectile
{
	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
		Projectile.aiStyle = -1;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 150;
		Projectile.extraUpdates = 1;
	}

	/*public void DoTrailCreation(TrailManager trailManager)
	{
		float strength = Projectile.ai[0] == 1 ? 1 : 0.5f;
		float width = Projectile.ai[0] == 1 ? 1 : 0.75f;
		trailManager.CreateTrail(Projectile, new OpacityUpdatingTrail(Projectile, Color.LightGoldenrodYellow.Additive(), Color.OrangeRed.Additive() * 0.5f * strength), new RoundCap(), new DefaultTrailPosition(), 15 * width, 200 * strength, new DefaultShader());
	}*/

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation() - Pi / 2;
		Lighting.AddLight(Projectile.position, Color.LightGoldenrodYellow.ToVector3() / 2);

		if (Projectile.velocity.Length() > 10)
			Projectile.velocity *= 0.97f;

		Projectile.velocity *= 0.99f;

		if(Projectile.timeLeft < 50)
			Projectile.alpha = (int)Min(Projectile.alpha + 6, 255);

		if (rand.NextFloat() < EaseQuadOut.Ease(Projectile.timeLeft / 150f))
		{
			Vector2 particleCenter = Projectile.Center;
			Vector2 particleVel = Vector2.Normalize(Projectile.velocity) * rand.NextFloat(2);
			Color lightColor = Color.LightGoldenrodYellow.Additive();
			Color darkColor = Color.Lerp(Color.LightGoldenrodYellow, Color.OrangeRed, 0.5f).Additive() * 0.5f;
			float scale = rand.NextFloat(0.3f, 0.4f);
			int lifeTime = rand.Next(20, 40);
			static void delegateAction(Particle p) => p.Velocity *= 0.97f;

			ParticleHandler.SpawnParticle(new GlowParticle(particleCenter, particleVel, lightColor, darkColor, scale, lifeTime, 3, delegateAction));
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Projectile.damage = (int)(Projectile.damage * 0.75f); 
		ParticleHandler.SpawnParticle(new LightBurst(target.Center, rand.NextFloatDirection(), Color.LightGoldenrodYellow.Additive(), 0.6f, 30));
		float vfxStrength = Projectile.ai[0] == 1 ? 1 : 0.75f;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				target.Center - Projectile.velocity,
				Color.LightGoldenrodYellow.Additive(),
				Color.Lerp(Color.OrangeRed.Additive(), Color.LightGoldenrodYellow.Additive(), 0.5f),
				1,
				90 * vfxStrength,
				40,
				"supPerlin",
				new Vector2(2, 1.5f),
				EaseCircularOut,
				false,
				0.6f) { Velocity = Projectile.velocity / 12 }.WithSkew(0.7f, Projectile.rotation + PiOver2 + rand.NextFloat(-0.3f, 0.3f)));

		for(int i = 0; i < 6 * vfxStrength; i++)
		{
			Vector2 particleCenter = target.Center;
			particleCenter += Vector2.Normalize(Projectile.velocity).RotatedBy(PiOver2) * rand.NextFloat(-15, 15);
			Vector2 particleVel = Vector2.Normalize(Projectile.velocity) * rand.NextFloat(0.5f, 3) * vfxStrength;
			Color lightColor = Color.LightGoldenrodYellow.Additive();
			Color darkColor = Color.Lerp(Color.LightGoldenrodYellow, Color.OrangeRed, 0.5f).Additive() * 0.5f;
			float scale = rand.NextFloat(0.4f, 0.5f);
			int lifeTime = (int)(rand.Next(20, 40) * vfxStrength);
			static void delegateAction(Particle p, float offset)
			{
				float progress = (EaseCircularOut.Ease(p.Progress) + offset) * TwoPi * 2;
				p.Position += Vector2.Normalize(p.Velocity).RotatedBy(progress);
				p.Velocity *= 0.96f;
			}

			float cycleOffset = rand.NextFloat();
			ParticleHandler.SpawnParticle(new GlowParticle(particleCenter, particleVel, lightColor * 0.75f, darkColor * 0.75f, scale, lifeTime, lifeTime / 2, p => delegateAction(p, cycleOffset)));
		}
	}

	public override void OnKill(int timeLeft)
	{
		if(timeLeft > 0)
		{

			ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, rand.NextFloatDirection(), Color.LightGoldenrodYellow.Additive(), 0.6f, 30));
			float vfxStrength = Projectile.ai[0] == 1 ? 1 : 0.75f;

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
					Projectile.Center - Projectile.oldVelocity,
					Color.LightGoldenrodYellow.Additive(),
					Color.Lerp(Color.OrangeRed.Additive(), Color.LightGoldenrodYellow.Additive(), 0.5f),
					1,
					90 * vfxStrength,
					40,
					"supPerlin",
					new Vector2(2, 1.5f),
					EaseCircularOut,
					false,
					0.6f) { Velocity = Projectile.oldVelocity / 6 }.WithSkew(0.7f, Projectile.rotation + PiOver2));

			for (int i = 0; i < 12 * vfxStrength; i++)
			{
				Vector2 particleCenter = Projectile.Center;
				particleCenter += Vector2.Normalize(Projectile.oldVelocity).RotatedBy(PiOver2) * rand.NextFloat(-15, 15);
				Vector2 particleVel = Vector2.Normalize(Projectile.oldVelocity) * rand.NextFloat(0.5f, 3) * vfxStrength;
				Color lightColor = Color.LightGoldenrodYellow.Additive();
				Color darkColor = Color.Lerp(Color.LightGoldenrodYellow, Color.OrangeRed, 0.5f).Additive() * 0.5f;
				float scale = rand.NextFloat(0.4f, 0.5f);
				int lifeTime = (int)(rand.Next(30, 50) * vfxStrength);
				static void delegateAction(Particle p, float offset)
				{
					float progress = (EaseCircularOut.Ease(p.Progress) + offset) * TwoPi * 2;
					p.Position += Vector2.Normalize(p.Velocity).RotatedBy(progress);
					p.Velocity *= 0.96f;
				}

				float cycleOffset = rand.NextFloat();
				ParticleHandler.SpawnParticle(new GlowParticle(particleCenter, particleVel, lightColor * 0.75f, darkColor * 0.75f, scale, lifeTime, lifeTime / 2, p => delegateAction(p, cycleOffset)));
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Color color = Color.LightGoldenrodYellow.Additive();

		ConeNoise(-10, 0.5f, 5);

		Projectile.QuickDraw(drawColor: Projectile.GetAlpha(lightColor));

		GlowmaskProjectile.ProjIdToGlowmask.TryGetValue(Type, out GlowmaskInfo glowmaskInfo);
		Texture2D glowmaskTex = glowmaskInfo.Glowmask.Value;

		for (int i = 0; i < 6; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy((TwoPi * i / 6) + Projectile.rotation);

			spriteBatch.Draw(glowmaskTex, Projectile.Center + offset - screenPosition, null, color * EaseQuadIn.Ease(Projectile.Opacity) * 0.33f, Projectile.rotation, glowmaskTex.Size()/2, Projectile.scale, SpriteEffects.None, 0);
		}

		spriteBatch.RestartToDefault();

		ConeNoise(10, 1, 0);

		return false;
	}

	private void ConeNoise(float spiral, float opacity, int timeOffset)
	{
		if (Projectile.timeLeft == 150)
			return;

		Effect effect = AssetLoader.LoadedShaders["SpiralNoiseCone"].Value;
		Texture2D texture = AssetLoader.LoadedTextures["swirlNoise"].Value;
		effect.Parameters["uTexture"].SetValue(texture);
		float easedProgress = EaseCircularIn.Ease(Projectile.timeLeft / 150f) * 3;
		effect.Parameters["scroll"].SetValue(new Vector2(spiral, easedProgress - timeOffset));

		effect.Parameters["uColor"].SetValue(Color.LightGoldenrodYellow.Additive(150).ToVector4());
		effect.Parameters["uColor2"].SetValue(Color.OrangeRed.Additive(150).ToVector4());

		effect.Parameters["textureStretch"].SetValue(new Vector2(1.5f, 0.5f) * 0.4f);
		effect.Parameters["texExponentRange"].SetValue(new Vector2(3, 1));

		effect.Parameters["finalIntensityMod"].SetValue(opacity);
		effect.Parameters["textureStrength"].SetValue(3 * EaseCircularIn.Ease(Projectile.Opacity) * EaseQuadOut.Ease(Projectile.timeLeft / 150f));
		effect.Parameters["finalExponent"].SetValue(1.75f);
		effect.Parameters["flipCoords"].SetValue(true);

		var dimensions = new Vector2(70, 120);
		dimensions = Vector2.Lerp(dimensions, new Vector2(80, 160), Projectile.ai[0]);

		var square = new SquarePrimitive
		{
			Color = Color.LightGoldenrodYellow.Additive() * Projectile.Opacity,
			Height = dimensions.X,
			Length = dimensions.Y,
			Position = Projectile.Center - screenPosition - 10 * Vector2.Normalize(Projectile.velocity),
			Rotation = Projectile.rotation - Pi,
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}
}
