using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract class RapierProjectile : SwungProjectile
{
	public readonly record struct RapierConfiguration(EaseFunction Easing, int Reach, int Width, int SweetSpotScale, int ParryWindow) : IConfiguration;

	public static readonly SoundStyle DefaultSwing = new("SpiritReforged/Assets/SFX/Projectile/SwordSlash1")
	{
		Pitch = 1f,
		PitchVariance = 0.15f
	};

	public float FreeDodgeTime => Main.player[Projectile.owner].GetModPlayer<FreeDodgePlayer>().freeDodgeTime.ApplyTo(GetConfig<RapierConfiguration>().ParryWindow);

	protected bool hitSweetSpot;

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (InSweetSpot(target, GetConfig<RapierConfiguration>().SweetSpotScale))
		{
			SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.4f }, target.Center);

			modifiers.SetCrit(); //Sweet spot crit
			hitSweetSpot = true;

			Vector2 direction = (-Projectile.velocity).RotatedBy(Main.rand.NextFromList(-1f, -0.5f, 0.5f, 1f));
			ParticleHandler.SpawnParticle(new CartoonHit(GetEndPosition() + direction * 20, 10, Main.rand.NextFloat(0.5f, 1.5f), direction.ToRotation() - MathHelper.PiOver2 - MathHelper.PiOver4, direction * Main.rand.NextFloat(1, 2), Color.White));
		}
		else
		{
			modifiers.DisableCrit();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float offset = Math.Max(30 * (0.5f - Progress * 2), -2);
		DrawHeld(lightColor, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

		return false;
	}

	public override bool? CanDamage() => (Counter <= 5) ? null : false;

	#region helpers
	public bool InSweetSpot(NPC target, int scale)
	{
		float reach = GetConfig<RapierConfiguration>().Reach - scale;
		return Projectile.Center.DistanceSQ(target.Hitbox.ClosestPointInRect(Projectile.Center)) > reach * reach;
	}

	public void DrawStar(Color lightColor, float scale, float intensity)
	{
		Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
		Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
		Vector2 position = GetEndPosition() - Main.screenPosition;

		Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.SteelBlue).Additive() * intensity, 0, star.Size() / 2, Projectile.scale * scale * intensity, default);
		Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.White).Additive() * intensity, 0, star.Size() / 2, Projectile.scale * 0.8f * scale * intensity, default);
	}

	/*//Adapted from MotionNoiseCone.cs
	public void DrawNoiseCone(Vector2 position, float progress, float rotation, int length, int width, Color darkColor, Color brightColor, int colorCount = 20, float intensity = 1.2f)
	{
		if (progress > 1)
			return;

		Effect effect = AssetLoader.LoadedShaders["MotionNoiseCone"].Value;
		Texture2D texture = AssetLoader.LoadedTextures["vnoise"].Value;

		effect.Parameters["uTexture"].SetValue(texture);
		effect.Parameters["Tapering"].SetValue(new Vector2(1f, 0.8f));
		effect.Parameters["scroll"].SetValue(-1.5f * (EaseFunction.EaseCircularOut.Ease(progress) + Counter / 60f));

		var dissipation = new Vector3(EaseFunction.EaseQuadIn.Ease(progress), 3f, 1.2f);
		effect.Parameters["dissipation"].SetValue(dissipation);

		effect.Parameters["uColor"].SetValue(darkColor.ToVector4());
		effect.Parameters["uColor2"].SetValue(Color.Lerp(darkColor, brightColor, 1 - dissipation.X).ToVector4());
		effect.Parameters["textureStretch"].SetValue(new Vector2(length / 1000f, width / 200f));
		effect.Parameters["texExponentLerp"].SetValue(new Vector3(0.01f, 40f, 2.25f));

		float easedProgress = EaseFunction.EaseQuadIn.Ease(progress);
		var xDistExponent = new Vector2(MathHelper.Lerp(0.15f, 0.5f, easedProgress), MathHelper.Lerp(2.5f, 4f, easedProgress));

		effect.Parameters["xDistExponent"].SetValue(xDistExponent);
		effect.Parameters["numColors"].SetValue(colorCount);
		effect.Parameters["colorLerpExponent"].SetValue(1.5f);
		effect.Parameters["finalIntensityMod"].SetValue(intensity);

		Color lightColor = Lighting.GetColor(position.ToTileCoordinates());

		var square = new SquarePrimitive
		{
			Color = lightColor * (1 - dissipation.X),
			Length = length,
			Height = width,
			Position = position - Main.screenPosition,
			Rotation = rotation + MathHelper.Pi,
		};
		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}*/
	#endregion
}