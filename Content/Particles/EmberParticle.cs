using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class EmberParticle : Particle
{
	private const float FADETIME = 0.3f;

	public bool emitLight = true;

	private readonly Color _startColor;
	private readonly Color _endColor;
	private ParticleLayer _drawLayer = ParticleLayer.BelowProjectile;
	private readonly Vector2[] oldPositions = [];

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public override ParticleLayer DrawLayer => _drawLayer;

	public EmberParticle OverrideDrawLayer(ParticleLayer newLayer)
	{
		_drawLayer = newLayer;
		return this;
	}

	public EmberParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float scale, int maxTime, int maxTrailLength = 1)
	{
		Position = position;

		oldPositions = new Vector2[maxTrailLength];
		for (int i = 0; i < oldPositions.Length; i++)
			oldPositions[i] = position;

		Velocity = velocity;
		_startColor = startColor;
		_endColor = endColor;
		Scale = scale;
		MaxTime = maxTime;
	}

	public EmberParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, int maxTrailLength = 1) : this(position, velocity, color, color, scale, maxTime, maxTrailLength) { }

	public override void Update()
	{
		float fadeintime = MaxTime * FADETIME;
		Color = Color.Lerp(_startColor, _endColor, Progress);

		if (TimeActive < fadeintime)
			Color *= TimeActive / fadeintime;
		else if (TimeActive > MaxTime - fadeintime)
			Color *= (MaxTime - TimeActive) / fadeintime;

		if (emitLight)
			Lighting.AddLight(Position, Color.ToVector3() * Scale * 0.5f);

		Velocity = Velocity.RotatedByRandom(0.1f);
		Velocity *= 0.99f;

		oldPositions[0] = Position;
		for(int i = oldPositions.Length - 1; i > 0; i--)
			oldPositions[i] = oldPositions[i - 1];
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var tex = ParticleHandler.GetTexture(Type);
		float scaleTimeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;
			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			float opacity = easeModifier * 0.25f;

			var position = oldPositions[i] - Main.screenPosition;

			spriteBatch.Draw(tex, position, null, Color.Additive() * opacity, 0, tex.Size() / 2, easeModifier * Scale * scaleTimeModifier, default, 0);
			spriteBatch.Draw(tex, position, null, Color.Lerp(Color, Color.White, 0.5f).Additive() * opacity * 3, 0, tex.Size() / 2, easeModifier * Scale * scaleTimeModifier * 0.5f, default, 0);
		}
	}
}