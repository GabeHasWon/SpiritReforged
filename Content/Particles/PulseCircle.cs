using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;

namespace SpiritReforged.Content.Particles;

public class PulseCircle : Particle
{
	/// <summary>
	/// The rotation along the 2D plane of the particle, defaults to 0
	/// </summary>
	public float Angle { get; set; } = 0;

	/// <summary>
	/// The rotation into the 3D plane(makes the particle thin out and become darker the "further" it is from the camera for a pseudo 3D effect)<br />
	/// Goes from 0-1, 0 being default, and 1 being a 0 pixel thin line
	/// </summary>
	public float ZRotation { get; set; } = 0;
	public bool UseLightColor { get; set; } = false;

	protected Entity entity;
	private Vector2 _offset;

	private float _opacity;
	private readonly float _maxRadius;
	private readonly EaseFunction _easeType;
	private readonly bool _inversePulse;
	private readonly Color _bloomColor;
	private readonly float _ringWidth;
	private readonly float _endRingWidth;

	public PulseCircle(Vector2 position, Color ringColor, Color bloomColor, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0)
	{
		Position = position;
		Color = ringColor;
		_bloomColor = bloomColor;
		_maxRadius = maxRadius;
		MaxTime = maxTime;
		_easeType = MovementStyle ?? EaseFunction.Linear;
		_inversePulse = inverted;
		_ringWidth = ringWidth;
		_endRingWidth = endRingWidth;
	}

	public PulseCircle(Vector2 position, Color color, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : this(position, color, color * 0.25f, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth) { }

	public override void Update()
	{
		if (entity != null)
		{
			if (!entity.active)
			{
				Kill();
				return;
			}

			Position = entity.Center + (_offset += Velocity);
		}
		else
		{
			Position += Velocity;
		}

		float progress = GetProgress();

		Scale = _maxRadius * progress;
		_opacity = Math.Min(3 * (1 - progress), 1f);
	}

	private float GetProgress()
	{
		float newProgress = Progress;
		if (_inversePulse)
			newProgress = 1 - newProgress;

		newProgress = _easeType.Ease(newProgress);
		return newProgress;
	}

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	internal virtual string EffectPassName => "GeometricStyle";

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Effect effect = AssetLoader.LoadedShaders["PulseCircle"];
		effect.Parameters["RingColor"].SetValue(Color.ToVector4());
		effect.Parameters["BloomColor"].SetValue(_bloomColor.ToVector4());
		effect.Parameters["RingWidth"].SetValue(_ringWidth * MathHelper.Lerp(1, _endRingWidth, 1 - EaseFunction.EaseCubicIn.Ease(_opacity)));
		EffectExtras(ref effect);
		Color lightColor = Color.White;
		if (UseLightColor)
			lightColor = Lighting.GetColor(Position.ToTileCoordinates().X, Position.ToTileCoordinates().Y);

		var square = new SquarePrimitive
		{
			Color = lightColor * EaseFunction.EaseCubicOut.Ease(_opacity),
			Height = Scale,
			Length = Scale * (1 - ZRotation),
			Position = Position - Main.screenPosition,
			Rotation = Angle + MathHelper.Pi,
			ColorXCoordMod = 1 - ZRotation
		};
		PrimitiveRenderer.DrawPrimitiveShape(square, effect, EffectPassName);
	}

	internal virtual void EffectExtras(ref Effect curEffect) { }

	public PulseCircle WithSkew(float zRotation, float rotation)
	{
		ZRotation = zRotation;
		Angle = rotation;
		return this;
	}

	public PulseCircle UsesLightColor()
	{
		UseLightColor = true;
		return this;
	}

	public PulseCircle Attach(Entity entity, bool center = false)
	{
		this.entity = entity;

		if (center)
			Position = entity.Center;

		_offset = Position - entity.Center;
		return this;
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;
}
