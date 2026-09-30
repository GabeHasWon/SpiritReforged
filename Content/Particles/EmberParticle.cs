using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class EmberParticle : Particle
{
	private const float FADETIME = 0.3f;

	public bool emitLight = true;

	private readonly Color _startColor;
	private readonly Color _endColor;
	private Action<Particle> _extraAction;
	private readonly Vector2[] oldPositions = [];

	public Color Color;

	public EmberParticle AddAction(Action<Particle> action)
	{
		_extraAction = action;
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

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

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

		_extraAction?.Invoke(this);

		if (oldPositions.Length != 0)
		{
			oldPositions[0] = Position;
			for (int i = oldPositions.Length - 1; i > 0; i--)
				oldPositions[i] = oldPositions[i - 1];
		}
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		float scaleTimeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;
			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			float opacity = easeModifier * 0.25f;

			var position = oldPositions[i] - Main.screenPosition;

			spriteBatch.Draw(texture, position, null, Color.Additive() * opacity, 0, texture.Size() / 2, easeModifier * Scale * scaleTimeModifier, default, 0);
			spriteBatch.Draw(texture, position, null, Color.Lerp(Color, Color.White, 0.5f).Additive() * opacity * 3, 0, texture.Size() / 2, easeModifier * Scale * scaleTimeModifier * 0.5f, default, 0);
		}
	}
}

public class CurvingEmberParticle : Particle
{
	private const float FADETIME = 0.3f;

	public bool emitLight = true;

	private int _time;
	private int _direction;
	private int _timeBetweenCurving;

	public float rotationalStrength = 0.04f;

	private readonly Color _startColor;
	private readonly Color _endColor;
	private readonly Vector2[] oldPositions = [];

	public Color Color;

	public CurvingEmberParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float scale, int maxTime, int direction, int timeToCurve, int maxTrailLength = 1)
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

		_direction = direction;
		_timeBetweenCurving = timeToCurve;

		_time = 0;
	}

	public CurvingEmberParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, int direction, int timeToCurve, int maxTrailLength = 1) : this(position, velocity, color, color, scale, maxTime, direction, timeToCurve, maxTrailLength) { }

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);
		_time++;

		if (_time % _timeBetweenCurving == 0)
			_direction *= -1;

		float fadeintime = MaxTime * FADETIME;
		Color = Color.Lerp(_startColor, _endColor, Progress);

		if (TimeActive < fadeintime)
			Color *= TimeActive / fadeintime;
		else if (TimeActive > MaxTime - fadeintime)
			Color *= (MaxTime - TimeActive) / fadeintime;

		if (emitLight)
			Lighting.AddLight(Position, Color.ToVector3() * Scale * 0.5f);

		Velocity = Velocity.RotatedBy(rotationalStrength * _direction);
		Velocity *= 0.99f;

		if (oldPositions.Length != 0)
		{
			oldPositions[0] = Position;
			for (int i = oldPositions.Length - 1; i > 0; i--)
				oldPositions[i] = oldPositions[i - 1];
		}
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		float scaleTimeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;
			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			float opacity = easeModifier * 0.25f;

			var position = oldPositions[i] - Main.screenPosition;

			spriteBatch.Draw(texture, position, null, Color.Additive() * opacity, 0, texture.Size() / 2, easeModifier * Scale * scaleTimeModifier, default, 0);
			spriteBatch.Draw(texture, position, null, Color.Lerp(Color, Color.White, 0.5f).Additive() * opacity * 3, 0, texture.Size() / 2, easeModifier * Scale * scaleTimeModifier * 0.5f, default, 0);
		}
	}
}