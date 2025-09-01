using SpiritReforged.Common.Easing;

namespace SpiritReforged.Content.Particles;

internal class BasicNoiseCone(Vector2 position, Vector2 velocity, int maxTime, Point dimensions = default) : MotionNoiseCone(position, velocity, (dimensions.X == 0) ? 100 : dimensions.X, (dimensions.Y == 0) ? 100 : dimensions.Y, velocity.ToRotation(), maxTime)
{
	private Color _bright;
	private Color _dark;

	private int _numColors = 8;
	private float _colorLerpExponent = 1.5f;
	private float _intensity = 1.2f;

	public BasicNoiseCone SetColors(Color brightColor, Color darkColor)
	{
		_bright = brightColor;
		_dark = darkColor;

		return this;
	}

	public BasicNoiseCone SetNumColors(int value)
	{
		_numColors = value;
		return this;
	}

	public BasicNoiseCone SetColorLerpExponent(float value)
	{
		_colorLerpExponent = value;
		return this;
	}

	public BasicNoiseCone SetIntensity(float value)
	{
		_intensity = value;
		return this;
	}

	internal override Color BrightColor => _bright;
	internal override Color DarkColor => _dark;

	internal override float ColorLerpExponent => _colorLerpExponent;
	internal override int NumColors => _numColors;
	internal override float FinalIntensity => _intensity;

	internal override bool UseLightColor => true;

	internal override float GetScroll() => -1.5f * (EaseFunction.EaseCircularOut.Ease(Progress) + TimeActive / 60f);

	internal override void DissipationStyle(ref float dissipationProgress, ref float finalExponent, ref float xCoordExponent)
	{
		dissipationProgress = EaseFunction.EaseQuadIn.Ease(Progress);
		finalExponent = 3f;
		xCoordExponent = 1.2f;
	}

	internal override void TaperStyle(ref float totalTapering, ref float taperExponent)
	{
		totalTapering = 1;
		taperExponent = 0.8f;
	}

	internal override void TextureExponent(ref float minExponent, ref float maxExponent, ref float lerpExponent)
	{
		minExponent = 0.01f;
		maxExponent = 40f;
		lerpExponent = 2.25f;
	}

	internal override void XDistanceFade(ref float centeredPosition, ref float exponent)
	{
		float easedProgress = EaseFunction.EaseQuadIn.Ease(Progress);
		centeredPosition = MathHelper.Lerp(0.15f, 0.5f, easedProgress);
		exponent = MathHelper.Lerp(2.5f, 4f, easedProgress);
	}
}