using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class LightFlash : Particle
{
	private Color startColor;
	private Color endColor;
	private Vector2 scale;

	private readonly Action<Particle> _action;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public LightFlash(Vector2 position, Color StartColor, Color EndColor, Vector2 Scale, int maxTime, float rotation, Action<Particle> extraUpdateAction = null)
	{
		Position = position;
		startColor = StartColor;
		endColor = EndColor;
		Rotation = rotation;
		scale = Scale;
		MaxTime = maxTime;
		_action = extraUpdateAction;
	}

	public LightFlash(Vector2 position, Color color, Vector2 Scale, int maxTime, float rotation, Action<Particle> extraUpdateAction = null) : this(position, color, color, Scale, maxTime, rotation, extraUpdateAction) { }

	public override void Update()
	{
		Color = Color.Lerp(startColor, endColor, Progress);
		Velocity = Vector2.Zero;

		_action?.Invoke(this);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D tex = AssetLoader.LoadedTextures["ShineAlpha"].Value;
		float progress = 1f - Progress;

		spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * progress, Rotation, new Vector2(tex.Width / 2, tex.Height), scale, SpriteEffects.None, 0);
	}
}
