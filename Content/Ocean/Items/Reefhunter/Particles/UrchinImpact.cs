using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;

public class UrchinImpact(Vector2 position, Vector2 velocity, float width, float length, float rotation, int maxTime, float opacity) : MotionNoiseCone(position, velocity, width, length, rotation, maxTime)
{
	public override bool UseLightColor => true;
	private readonly float _opacity = opacity;
	
	internal override float GetScroll() => TimeActive / 20f;

	public override Color BrightColor => new(145, 67, 111);
	public override Color DarkColor => new(87, 35, 88);

	internal override void DissipationStyle(ref float dissipationProgress, ref float finalExponent, ref float xCoordExponent)
	{
		dissipationProgress = (float)Math.Max(1.33f * (Progress - 0.25f), 0);
		finalExponent = 3f;
		xCoordExponent = 1.5f;
	}

	internal override float ColorLerpExponent => 2f;

	internal override int NumColors => 12;

	internal override float FinalIntensity => 1.25f * _opacity;

	internal override void TaperStyle(ref float totalTapering, ref float taperExponent)
	{
		totalTapering = 1;
		taperExponent = 1.2f;
	}

	internal override void TextureExponent(ref float minExponent, ref float maxExponent, ref float lerpExponent)
	{
		minExponent = 0.01f;
		maxExponent = 30f;
		lerpExponent = 2.5f;
	}

	internal override void XDistanceFade(ref float centeredPosition, ref float exponent)
	{
		centeredPosition = 0.15f;
		exponent = 2f;
	}
}
