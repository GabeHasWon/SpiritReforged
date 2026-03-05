using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;
namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class AdornedBowHeld() : BaseChargeBow(2, 1.5f, 30)
{
	internal bool _flashed;
	internal int _flashTimer;
	public override void SetStringDrawParams(out float stringLength, out float maxDrawback, out Vector2 stringOrigin, out Color stringColor)
	{
		stringLength = 30;
		maxDrawback = 10;
		stringOrigin = new Vector2(5, 25);
		stringColor = new Color(255, 234, 93);
	}

	protected override void ModifyFiredProj(ref Projectile projectile, bool fullCharge, bool perfectShot)
	{
		SoundStyle whoosh = new("SpiritReforged/Assets/SFX/Item/GenericClubWhoosh")
		{
			Volume = 0.5f,
			PitchVariance = 0.15f
		};

		if (!Main.dedServ)
			SoundEngine.PlaySound(whoosh, projectile.Center);

		if (perfectShot)
		{
			projectile.GetGlobalProjectile<AdornedArrowHandler>().active = true;
			projectile.velocity *= 1.5f;

			if (Main.myPlayer == projectile.owner)
			{
				// Client side screen shake
				Vector2 dir = projectile.rotation.ToRotationVector2().RotatedByRandom(0.3f);

				Main.instance.CameraModifiers.Add(new PunchCameraModifier(projectile.Center, dir * Main.rand.NextFloat(1f, 2f), 2, 1, 10, -1, "AdornedBowChargedShot"));
			}

			SoundStyle perfectFlash = new("SpiritReforged/Assets/SFX/Item/GenericClubWhoosh")
			{
				Volume = 0.5f,
				PitchVariance = 0.15f
			};

			if (!Main.dedServ)
				SoundEngine.PlaySound(whoosh, projectile.Center);

			Color color = Color.Orange;

			Vector2 pos = projectile.Center + Projectile.rotation.ToRotationVector2() * 25;

			ParticleHandler.SpawnParticle(new LightBurst(pos, Main.rand.NextFloatDirection(), color.Additive(), 0.5f, 35));
			ParticleHandler.SpawnParticle(new LightBurst(pos, Main.rand.NextFloatDirection(), Color.White.Additive(), 0.5f, 35));
		}

		if (fullCharge)
		{
			float ringSize = 70 + (perfectShot ? 30 : 0);
			int ringTime = 25 + (perfectShot ? 5 : 0);
			float ringOpacity = 0.8f + (perfectShot ? 0.2f : 0);
			float velocity = perfectShot ? 0.4f : 0.25f;

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				Projectile.Center + Projectile.rotation.ToRotationVector2() * 10,
				Color.LightGoldenrodYellow.Additive() * ringOpacity,
				Color.Lerp(Color.OrangeRed.Additive(), Color.LightGoldenrodYellow.Additive(), 0.5f) * ringOpacity,
				1f,
				ringSize,
				ringTime,
				"supPerlin",
				new Vector2(2, 1.5f),
				EaseCircularOut,
				false,
				0.8f) { Velocity = Projectile.rotation.ToRotationVector2() * velocity } .WithSkew(0.85f, Projectile.rotation));

			if(perfectShot)
			{
				ParticleHandler.SpawnParticle(new TexturedPulseCircle(
					Projectile.Center + Projectile.rotation.ToRotationVector2() * 10,
					Color.LightGoldenrodYellow.Additive() * ringOpacity,
					Color.Lerp(Color.OrangeRed.Additive(), Color.LightGoldenrodYellow.Additive(), 0.5f) * ringOpacity,
					1f,
					ringSize * 0.75f,
					ringTime - 7,
					"supPerlin",
					new Vector2(2, 1.5f),
					EaseCircularOut,
					false,
					0.6f) { Velocity = Projectile.rotation.ToRotationVector2() * velocity * 2f }.WithSkew(0.85f, Projectile.rotation));
			}
		}
	}

	public override void PostAI()
	{
		if (_flashTimer > 0)
			_flashTimer--;

		float radius = 2f * Charge / 1f;  // shakes rapidly whilst charging up a shot

		if (Charge == 1f)
		{
			if (!_flashed)
			{
				SoundStyle charged = new("SpiritReforged/Assets/SFX/Item/ClubReady")
				{
					Volume = 0.5f,
					PitchVariance = 0.15f
				};

				if (!Main.dedServ)
					SoundEngine.PlaySound(charged, Projectile.Center);

				_flashTimer = 10;
				_flashed = true;
			}

			if (_perfectShotCurTimer > 0) // shakes less whilst the window for a perfect shot closes
				radius = 2f * _perfectShotCurTimer / _perfectShotMaxTime;
			else                          // no longer shakes when the perfect shot window is over
				radius *= 0f;
		}
			

		if (_fired) // stop shaking when fired
			radius *= Projectile.timeLeft / 30f;

		Projectile.Center += Main.rand.NextVector2Circular(radius, radius);
	}

	public override void PostDraw(Color lightColor)
	{
		GlowmaskProjectile.ProjIdToGlowmask.TryGetValue(Type, out GlowmaskInfo glowmaskInfo);
		Texture2D glowmaskTex = glowmaskInfo.Glowmask.Value;
		Texture2D starTex = AssetLoader.LoadedTextures["Star"].Value;

		Color color = Color.White.Additive();
		float perfectShotProgress = EaseSine.Ease(EaseCircularOut.Ease(1 - _perfectShotCurTimer / _perfectShotMaxTime));
		float strength = Charge * (Projectile.timeLeft / 30f);

		int numGlow = 8;
		for (int i = 0; i < 6; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy((TwoPi * i / numGlow) + Projectile.rotation + Main.GlobalTimeWrappedHourly / 5) * Lerp(4, 2, strength);

			Main.spriteBatch.Draw(glowmaskTex, Projectile.Center + new Vector2(0f, Projectile.gfxOffY) + offset - Main.screenPosition, null, color * (EaseCircularIn.Ease(strength) + perfectShotProgress) * (1f / numGlow), Projectile.rotation, glowmaskTex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
		}

		var center = Projectile.Center + new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 10;
		float maxSize = 0.6f * Projectile.scale;

		Vector2 scale = new Vector2(1f, 1f) * Lerp(0, maxSize, perfectShotProgress) * 0.5f;
		var starOrigin = starTex.Size() / 2;
		Color starColor = Projectile.GetAlpha(Color.Lerp(Color.LightGoldenrodYellow.Additive(), Color.OrangeRed.Additive(), perfectShotProgress)) * EaseQuadOut.Ease(perfectShotProgress);
		Main.spriteBatch.Draw(starTex, center, null, starColor, Projectile.rotation, starOrigin, scale, SpriteEffects.None, 0);
	}

	protected override void DrawArrow(Texture2D arrowTex, Vector2 arrowPos, Vector2 arrowOrigin, float perfectShotProgress, Color lightColor)
	{
		float opacity = 1 - _perfectShotCurTimer / _perfectShotMaxTime;
		opacity = Math.Max(opacity, 1.5f * perfectShotProgress);

		if(Charge == 1)
			ConeNoise(-10, 0.5f * opacity, 10, perfectShotProgress);

		base.DrawArrow(arrowTex, arrowPos, arrowOrigin, perfectShotProgress, lightColor);

		if(Charge == 1)
		{
			Main.spriteBatch.RestartToDefault();

			ConeNoise(10, 0.5f * opacity, 0, perfectShotProgress);
		}
	}

	private void ConeNoise(float spiral, float opacity, int timeOffset, float perfectShotProgress)
	{
		Effect effect = AssetLoader.LoadedShaders["SpiralNoiseCone"].Value;
		Texture2D texture = AssetLoader.LoadedTextures["swirlNoise"].Value;
		effect.Parameters["uTexture"].SetValue(texture);
		effect.Parameters["scroll"].SetValue(new Vector2(spiral, (Main.GlobalTimeWrappedHourly / 5) - timeOffset));

		effect.Parameters["uColor"].SetValue(Color.LightGoldenrodYellow.Additive(150).ToVector4());
		effect.Parameters["uColor2"].SetValue(Color.OrangeRed.Additive(150).ToVector4());

		effect.Parameters["textureStretch"].SetValue(new Vector2(2, 0.3f) * 0.4f);
		effect.Parameters["texExponentRange"].SetValue(new Vector2(5, 0.1f));

		effect.Parameters["finalIntensityMod"].SetValue(opacity);
		effect.Parameters["textureStrength"].SetValue(4);
		effect.Parameters["finalExponent"].SetValue(1.5f);
		effect.Parameters["flipCoords"].SetValue(true);

		var dimensions = new Vector2(30, 80);
		dimensions = Vector2.Lerp(dimensions, new Vector2(40, 90), EaseQuadOut.Ease(perfectShotProgress));

		var square = new SquarePrimitive
		{
			Color = Color.LightGoldenrodYellow.Additive() * Projectile.Opacity,
			Height = dimensions.X,
			Length = dimensions.Y,
			Position = Projectile.Center + new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition + Vector2.UnitX.RotatedBy(Projectile.rotation) * 5,
			Rotation = Projectile.rotation + PiOver2,
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}
}