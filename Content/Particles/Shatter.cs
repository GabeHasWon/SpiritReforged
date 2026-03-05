using SpiritReforged.Common.Particle;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Particles;

public class Shatter : Particle
{
	Color _baseColor;
	float _baseScale;

	public Shatter(Vector2 position, Color baseColor, float scale, int maxTime)
	{
		Position = position;
		Rotation = Main.rand.NextFloat(TwoPi);
		MaxTime = maxTime;
		_baseScale = scale;
		_baseColor = baseColor;
	}

	public override void Update()
	{
		Color = _baseColor * EaseQuadOut.Ease(1 - Progress);
		Scale = Lerp(0.48f, 0.55f, EaseSine.Ease(EaseCubicOut.Ease(Progress))) * _baseScale;
	}

	public override ParticleDrawType DrawType => ParticleDrawType.CustomBatchedAdditiveBlend;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
		lightColor = Color.Lerp(Color, Color.MultiplyRGBA(lightColor), 0.5f);
		Texture2D gradient = AssetLoader.LoadedTextures["Bloom"].Value;
		spriteBatch.Draw(gradient, Position - Main.screenPosition, null, _baseColor.MultiplyRGB(lightColor) * EaseCubicIn.Ease(1 - Progress) * .75f, 0, gradient.Size() / 2, Scale, SpriteEffects.None, 0);

		DrawHelpers.DrawChromaticAberration(Vector2.UnitX, 1, delegate (Vector2 offset, Color colorMod)
		{
			spriteBatch.Draw(Texture, Position - Main.screenPosition + offset, null, lightColor.MultiplyRGB(colorMod), Rotation, Texture.Size() / 2, Scale, SpriteEffects.None, 0);

			float opacity = 0.3f;
			float scale = Lerp(1.25f, 1f, Progress);
			spriteBatch.Draw(Texture, Position - Main.screenPosition + offset, null, lightColor.MultiplyRGB(colorMod) * opacity, Rotation + Pi, Texture.Size() / 2, Scale * scale, SpriteEffects.None, 0);
		});
	}
}
