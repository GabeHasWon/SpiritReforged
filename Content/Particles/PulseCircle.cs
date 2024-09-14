﻿using SpiritReforged.Common.Easing;
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

	private float _opacity;

	private readonly float _maxRadius;
	private readonly Entity _entity;
	private readonly EaseFunction _easeType;
	private readonly bool _inversePulse;
	private readonly Color _bloomColor;
	private Vector2 _offset = Vector2.Zero;
	private readonly float _ringWidth;

	public PulseCircle(Vector2 position, Color ringColor, Color bloomColor, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, bool inverted = false)
	{
		Position = position;
		Color = ringColor;
		_bloomColor = bloomColor;
		_maxRadius = maxRadius;
		MaxTime = maxTime;
		_easeType = MovementStyle ?? EaseFunction.Linear;
		_inversePulse = inverted;
		_ringWidth = ringWidth;
	}

	public PulseCircle(Entity attatchedEntity, Color ringColor, Color bloomColor, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, Vector2? startingPosition = null, bool inverted = false) : this(attatchedEntity.Center, ringColor, bloomColor, ringWidth, maxRadius, maxTime, MovementStyle, inverted)
	{
		_entity = attatchedEntity;
		Position = _entity.Center;
		_offset = startingPosition != null ? startingPosition.Value - _entity.Center : Vector2.Zero;
	}

	public PulseCircle(Vector2 position, Color color, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, bool inverted = false) : this(position, color, color * 0.25f, ringWidth, maxRadius, maxTime, MovementStyle, inverted) { }

	public PulseCircle(Entity attatchedEntity, Color color, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, Vector2? startingPosition = null, bool inverted = false) : this(attatchedEntity, color, color * 0.25f, ringWidth, maxRadius, maxTime, MovementStyle, startingPosition, inverted) { }

	public override void Update()
	{
		if (_entity != null)
		{
			if (!_entity.active)
			{
				Kill();
				return;
			}

			_offset += Velocity;
			Position = _entity.Center + _offset;
		}
		else
			Position += Velocity;

		float Progress = GetProgress();

		Scale = _maxRadius * Progress;
		_opacity = Math.Min(2 * (1 - Progress), 1f);
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
		effect.Parameters["RingWidth"].SetValue(_ringWidth * EaseFunction.EaseCubicIn.Ease(_opacity));
		EffectExtras(ref effect);
		Color lightColor = Color.White;
		if (UseLightColor)
			lightColor = Lighting.GetColor(Position.ToTileCoordinates().X, Position.ToTileCoordinates().Y);

		var square = new SquarePrimitive
		{
			Color = lightColor * _opacity,
			Height = Scale,
			Length = Scale * (1 - ZRotation),
			Position = Position - Main.screenPosition,
			Rotation = Angle + MathHelper.Pi,
			ColorXCoordMod = 1 - ZRotation
		};
		PrimitiveRenderer.DrawPrimitiveShape(square, effect, EffectPassName);
	}

	internal virtual void EffectExtras(ref Effect curEffect)
	{

	}

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

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;
}
