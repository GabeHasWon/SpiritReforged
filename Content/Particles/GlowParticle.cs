using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class GlowParticle : Particle
{
	private readonly Color _startColor;
	private readonly Color _endColor;

	private const float FADETIME = 0.3f;

	private readonly Action<Particle> _action;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	private readonly Vector2[] oldPositions = [];

	private ParticleLayer _drawLayer = ParticleLayer.BelowProjectile;

	public GlowParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float scale, int maxTime, int maxTrailLength = 1, Action<Particle> extraUpdateAction = null)
	{
		Position = position;
		oldPositions = new Vector2[maxTrailLength];
		for (int i = 0; i < oldPositions.Length; i++)
			oldPositions[i] = position;

		Velocity = velocity;
		_startColor = startColor;
		_endColor = endColor;
		Scale = scale;
		_action = extraUpdateAction;
		MaxTime = maxTime;
	}
	public GlowParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, int maxTrailLength = 1, Action<Particle> extraUpdateAction = null) : this(position, velocity, color, color, scale, maxTime, maxTrailLength, extraUpdateAction) { }

	public override void Update()
	{
		float fadeintime = MaxTime * FADETIME;
		Color = Color.Lerp(_startColor, _endColor, Progress);
		if (TimeActive < fadeintime)
			Color *= TimeActive / fadeintime;

		else if(TimeActive > MaxTime - fadeintime)
			Color *= (MaxTime - TimeActive) / fadeintime;

		Lighting.AddLight(Position, Color.ToVector3() * Scale * 0.5f);

		_action?.Invoke(this);

		oldPositions[0] = Position;
		for(int i = oldPositions.Length - 1; i > 0; i--)
			oldPositions[i] = oldPositions[i - 1];
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D tex = ParticleHandler.GetTexture(Type);
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		float scaleTimeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;

			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			Draw(bloom, oldPositions[i], easeModifier * 0.2f, easeModifier * 0.15f);
		}

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;

			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			Draw(tex, oldPositions[i], easeModifier * 0.25f, easeModifier);
		}

		void Draw(Texture2D drawTex, Vector2 pos, float opacity, float scaleMod) => spriteBatch.Draw(drawTex, pos - Main.screenPosition, null, Color.Additive() * opacity, 0, drawTex.Size() / 2, scaleMod * Scale * scaleTimeModifier, SpriteEffects.None, 0);
	}

	public override ParticleLayer DrawLayer => _drawLayer;

	public GlowParticle OverrideDrawLayer(ParticleLayer newLayer)
	{
		_drawLayer = newLayer;
		return this;
	}
}
