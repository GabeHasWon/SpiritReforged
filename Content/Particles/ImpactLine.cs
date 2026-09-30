using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class ImpactLine : Particle
{
	/// <summary> Whether this particle should actually emit light. </summary>
	public bool NoLight { get; set; }

	public bool UseLightColor { get; set; }

	internal readonly Entity _ent = null;
	internal readonly Action<Particle> _action = null;

	internal Vector2 _scaleMod;
	internal Vector2 _offset;
	internal readonly float _acceleration;

	public Color Color { get; protected set; }

	public ImpactLine(Vector2 position, Vector2 velocity, Color color, Vector2 scale, int timeLeft, float acceleration, Entity attatchedEntity = null, Action<Particle> extraUpdateAction = null)
	{
		Position = position;
		Velocity = velocity;
		Color = color;
		_scaleMod = scale;
		MaxTime = timeLeft;
		_ent = attatchedEntity;
		_acceleration = acceleration;
		_action = extraUpdateAction;

		if (_ent != null)
			_offset = Position - _ent.Center;
	}

	public ImpactLine(Vector2 position, Vector2 velocity, Color color, Vector2 scale, int timeLeft, Entity attatchedEntity = null, Action<Particle> extraUpdateAction = null) : this(position, velocity, color, scale, timeLeft, 1, attatchedEntity, extraUpdateAction) { }

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		float opacity = EaseFunction.EaseQuadOut.Ease(EaseFunction.EaseSine.Ease(Progress));
		Color *= opacity;

		if (Velocity != Vector2.Zero)
			Rotation = Velocity.ToRotation() + MathHelper.PiOver2;

		if (!NoLight)
			Lighting.AddLight(Position, Color.ToVector3() / 2f);

		if (_ent != null)
		{
			if (!_ent.active)
			{
				ShouldBeRemovedFromRenderer = true;
				return;
			}

			Position = _ent.Center + _offset;
			_offset += Velocity;
		}

		_action?.Invoke(this);

		Velocity *= _acceleration;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		float progress = EaseFunction.EaseSine.Ease(Progress);
		var scale = new Vector2(0.5f, progress) * _scaleMod;
		var offset = Vector2.Zero;
		var tex = Texture;
		var origin = new Vector2(tex.Width / 2, tex.Height / 2);

		Color uColor = Color;
		if (UseLightColor)
			uColor = Color.MultiplyRGBA(Lighting.GetColor(Position.ToTileCoordinates()));

		spriteBatch.Draw(tex, Position + offset - Main.screenPosition, null, uColor * (progress / 5 + 0.8f), Rotation, origin, scale, SpriteEffects.None, 0);
	}
}
