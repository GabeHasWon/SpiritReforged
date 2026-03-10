using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.Audio;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;
namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class AdornedBowHeld() : BaseChargeBow(1.15f, 1.5f, 30)
{
	// TODO: change these sfx
	internal static SoundStyle ArrowShoot = new SoundStyle("SpiritReforged/Assets/SFX/Item/GenericClubWhoosh")
	{
		Volume = 0.5f,
		PitchVariance = 0.15f
	};

	internal static SoundStyle PerfectShot = new SoundStyle("SpiritReforged/Assets/SFX/Item/GenericClubWhoosh")
	{
		Volume = 0.5f,
		PitchVariance = 0.2f
	};

	internal static SoundStyle Flash = new("SpiritReforged/Assets/SFX/Item/ClubReady")
	{
		Volume = 0.5f,
		PitchVariance = 0.15f
	};

	internal const int MAX_FLASH_TIMER = 60;

	internal Color[] PrismaticColors;

	internal bool _flashed;
	internal int _flashTimer;
	public override void SetStringDrawParams(out float stringLength, out float maxDrawback, out Vector2 stringOrigin, out Color stringColor)
	{
		stringLength = 30;
		maxDrawback = 10;
		stringOrigin = new Vector2(5, 25);
		stringColor = Color.LightCyan;
	}

	protected override void ModifyFiredProj(ref Projectile projectile, bool fullCharge, bool perfectShot)
	{
		if (!Main.dedServ)
			SoundEngine.PlaySound(ArrowShoot, projectile.Center);

		if (perfectShot)
		{
			projectile.GetGlobalProjectile<AdornedArrowHandler>().active = true;
			projectile.velocity *= 1.5f;

			SoundStyle perfectFlash = new("SpiritReforged/Assets/SFX/Item/GenericClubWhoosh")
			{
				Volume = 0.5f,
				PitchVariance = 0.15f
			};

			if (!Main.dedServ)
				SoundEngine.PlaySound(PerfectShot, projectile.Center);
		}
	}

	public override void PostAI()
	{
		PrismaticColors ??= AdornedArrowHandler.GetPrismaticColors();

		if (_flashTimer > 0)
		{
			_flashTimer--;

			Lighting.AddLight(Projectile.Center, 
				AdornedArrowHandler.MulticolorLerp(_flashTimer / (float)MAX_FLASH_TIMER, [Color.Magenta, Color.Orange, Color.Cyan]).ToVector3()
				* 0.5f * (_flashTimer / (float)MAX_FLASH_TIMER));
		}
		
		float radius = 1.2f * Charge / 1f;  // shakes rapidly whilst charging up a shot

		if (Charge == 1f)
		{
			if (!_flashed)
			{
				if (!Main.dedServ)
					SoundEngine.PlaySound(Flash, Projectile.Center);

				_flashTimer = MAX_FLASH_TIMER;
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
		Texture2D starTex = AssetLoader.LoadedTextures["StarChromatic"].Value;

		Color color = (_flashTimer > 0f ? Color.Lerp(AdornedArrowHandler.MulticolorLerp(_flashTimer / (float)MAX_FLASH_TIMER, PrismaticColors), Color.LightSteelBlue, 1f - _flashTimer / (float)MAX_FLASH_TIMER) : Color.LightSteelBlue).Additive();
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

		Vector2 scale = new Vector2(0.15f, 0.15f) * Lerp(0, maxSize, perfectShotProgress) * 0.5f;
		var starOrigin = starTex.Size() / 2;
		Main.spriteBatch.Draw(starTex, center, null, color, Projectile.rotation, starOrigin, scale, SpriteEffects.None, 0);
	}

	protected override void DrawArrow(Texture2D arrowTex, Vector2 arrowPos, Vector2 arrowOrigin, float perfectShotProgress, Color lightColor)
	{
		float opacity = 1 - _perfectShotCurTimer / _perfectShotMaxTime;
		opacity = Math.Max(opacity, 1.5f * perfectShotProgress);

		if (Charge == 1)
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

		effect.Parameters["uColor"].SetValue(Color.LightSteelBlue.Additive(150).ToVector4());
		effect.Parameters["uColor2"].SetValue(Color.LightCyan.Additive(150).ToVector4());

		effect.Parameters["textureStretch"].SetValue(new Vector2(2, 0.3f) * 0.4f);
		effect.Parameters["texExponentRange"].SetValue(new Vector2(5, 0.1f));

		effect.Parameters["finalIntensityMod"].SetValue(opacity);
		effect.Parameters["textureStrength"].SetValue(4);
		effect.Parameters["finalExponent"].SetValue(1.5f);
		effect.Parameters["flipCoords"].SetValue(true)	;

		var dimensions = new Vector2(30, 80);
		dimensions = Vector2.Lerp(dimensions, new Vector2(40, 90), EaseQuadOut.Ease(perfectShotProgress));

		var square = new SquarePrimitive
		{
			Color =  (_flashTimer > 0f ? Color.Lerp(AdornedArrowHandler.MulticolorLerp(_flashTimer / (float)MAX_FLASH_TIMER, PrismaticColors), Color.LightSteelBlue, 1f - _flashTimer / (float)MAX_FLASH_TIMER) : Color.LightSteelBlue).Additive() * Projectile.Opacity,
			Height = dimensions.X,
			Length = dimensions.Y,
			Position = Projectile.Center + new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition + Vector2.UnitX.RotatedBy(Projectile.rotation) * 5,
			Rotation = Projectile.rotation + PiOver2,
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}
}