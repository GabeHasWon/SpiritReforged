using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;

public class BubbleParticle : Particle
{
	private readonly float _maxScale;
	private readonly Vector2 _initialVel;

	public BubbleParticle(Vector2 position, Vector2 velocity, float scale, int lifetime)
	{
		Position = position;
		Scale = scale;
		Color = Color.White.Additive();
		_maxScale = scale;
		MaxTime = lifetime;
		Velocity = velocity;
		_initialVel = velocity;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		Scale = MathHelper.Lerp(_maxScale, 0, EaseFunction.EaseCircularIn.Ease(Progress));
		Velocity = Vector2.Lerp(_initialVel, new Vector2(Main.windSpeedCurrent, -1), EaseFunction.EaseQuadOut.Ease(Progress) / 2) * (1 - EaseFunction.EaseCubicOut.Ease(Progress) / 4);
	}
}