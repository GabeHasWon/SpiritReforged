using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class SnowflakeParticle : Particle
{
	private const float FADETIME = 0.3f;

	public delegate void UpdateAction(Particle particle);
	private readonly UpdateAction _action;

	private readonly float _rotSpeed;
	private readonly int _style;
	private Color _startColor;
	private Color _endColor;

	public Color Color { get; protected set; }

	public SnowflakeParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float scale, int maxTime, float rotationSpeed = 1f, int typeValue = 1, UpdateAction action = null)
	{
		Position = position;
		Velocity = velocity;
		_startColor = startColor;
		_endColor = endColor;
		Scale = scale;
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		MaxTime = maxTime;
		_action = action;
		_rotSpeed = rotationSpeed;
		_style = typeValue; 
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);
		float fadeintime = MaxTime * FADETIME;

		Color = Color.Lerp(_startColor, _endColor, TimeActive / (float)MaxTime);
		Rotation += _rotSpeed * ((Velocity.X > 0) ? 0.07f : -0.07f);

		if (TimeActive < fadeintime)
			Color *= TimeActive / fadeintime;
		else if (TimeActive > MaxTime - fadeintime)
			Color *= (MaxTime - TimeActive) / fadeintime;

		Lighting.AddLight(Position, Color.ToVector3() * Scale * 0.5f);

		_action?.Invoke(this);
	}

	public override void Draw(ref ParticleRendererSettings settings,SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		var frame = texture.Frame(1, 3, frameY: _style);

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, (Color * 0.6f).Additive(), 0, bloom.Size() / 2, Scale / 4f, SpriteEffects.None, 0);
		spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(1, 3, frameY: Math.Min(_style, 2)), Color, Rotation, frame.Size() / 2, Scale, SpriteEffects.None, 0);
	}
}