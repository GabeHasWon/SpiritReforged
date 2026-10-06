using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Particles;

public class Shatter : Particle
{
	private Color _baseColor;
	private readonly float _baseScale;

	public Shatter(Vector2 position, Color baseColor, float scale, int maxTime)
	{
		Position = position;
		Rotation = Main.rand.NextFloat(TwoPi);
		MaxTime = maxTime;
		_baseScale = scale;
		_baseColor = baseColor;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		Color = _baseColor * EaseQuadOut.Ease(1 - Progress);
		Scale = Lerp(0.48f, 0.55f, EaseSine.Ease(EaseCubicOut.Ease(Progress))) * _baseScale;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
		lightColor = Color.Lerp(Color, Color.MultiplyRGBA(lightColor), 0.5f);
		Texture2D gradient = AssetLoader.LoadedTextures["Bloom"].Value;
		spriteBatch.Draw(gradient, Position + settings.AnchorPosition, null, _baseColor.MultiplyRGB(lightColor).Additive() * EaseCubicIn.Ease(1 - Progress) * 0.75f, 0, gradient.Size() / 2, Scale, SpriteEffects.None, 0);

		DrawHelpers.DrawChromaticAberration(Vector2.UnitX, 1, delegate (Vector2 offset, Color colorMod)
		{
			spriteBatch.Draw(Texture, Position - Main.screenPosition + offset, null, lightColor.MultiplyRGB(colorMod).Additive(), Rotation, Texture.Size() / 2, Scale, SpriteEffects.None, 0);

			float opacity = 0.3f;
			float scale = Lerp(1.25f, 1f, Progress);
			spriteBatch.Draw(Texture, Position - Main.screenPosition + offset, null, lightColor.MultiplyRGB(colorMod).Additive() * opacity, Rotation + Pi, Texture.Size() / 2, Scale * scale, SpriteEffects.None, 0);
		});
	}
}