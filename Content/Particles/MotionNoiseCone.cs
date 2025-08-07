using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;

namespace SpiritReforged.Content.Particles;

public abstract class MotionNoiseCone : Particle
{
	private readonly float _width;
	private readonly float _length;
	private Entity _attachedEntity = null;
	private Vector2 _offset = Vector2.Zero;
	private int _detachTime;

	public MotionNoiseCone(Vector2 position, Vector2 velocity, float width, float length, float rotation, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		MaxTime = maxTime;
		Rotation = rotation;
		_width = width;
		_length = length;
	}

	public MotionNoiseCone(Entity entity, Vector2 basePosition, Vector2 velocity, float width, float length, float rotation, int maxTime, int detatchTime = -1) : this(basePosition, velocity, width, length, rotation, maxTime)
	{
		if(entity == null)
			return;

		_attachedEntity = entity;
		_offset = basePosition - entity.Center;
		_detachTime = detatchTime;
	}

	public MotionNoiseCone AttachTo(Entity entity, int detachTime = -1)
	{
		_attachedEntity = entity;
		_offset = Position - entity.Center;
		_detachTime = detachTime;

		return this;
	}

	public override void Update()
	{
		if (TimeActive == _detachTime && _attachedEntity != null)
			_attachedEntity = null;

		if (_attachedEntity != null)
		{
			if (!_attachedEntity.active)
			{
				_attachedEntity = null;
				return;
			}

			Position = _attachedEntity.Center + _offset;
		}

		else
			Velocity *= 0.97f;
	}

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	internal virtual Color DarkColor { get; set; }
	internal virtual Color BrightColor { get; set; }
	internal virtual bool UseLightColor { get; set; }

	/// <summary>
	/// Controls the scrolling of the noise texture.
	/// </summary>
	/// <returns></returns>
	internal virtual float GetScroll() => Progress;

	/// <summary>
	/// Controls how the shader dissipates- the current progress, final exponent the shader is raised to, and how much the x coordinate affects it
	/// </summary>
	/// <param name="dissipationProgress">The current progress of dissipation. If not altered, defaults to the current progress of the particle.</param>
	/// <param name="finalExponent">The final exponent the shader is raised to over the course of the dissipation. If not altered, defaults to 1.</param>
	/// <param name="xCoordExponent">The exponent the distance between the current x coordinate and the dissipation is raised to. Defaults to 1, a lower exponent means the distance factor is more subtle.</param>
	internal virtual void DissipationStyle(ref float dissipationProgress, ref float finalExponent, ref float xCoordExponent) { }

	/// <summary>
	/// Controls the exponent the texture is raised to when first sampling it, and how much it increases or decreases based on the current x distance.
	/// </summary>
	/// <param name="minExponent">The exponent of the texture when x is 0.</param>
	/// <param name="maxExponent">The exponent of the texture when x is 1.</param>
	/// <param name="lerpExponent">The easing exponent of the interpolation between the min and max exponents.</param>
	internal virtual void TextureExponent(ref float minExponent, ref float maxExponent, ref float lerpExponent) { }
	/// <summary>
	/// How much the noise image is stretched. Divides the dimensions of the particle by this factor to get the adjusted noise coordinates.
	/// </summary>
	internal virtual Vector2 TextureStretch => new(1000, 200);

	/// <summary>
	/// Controls how the particle fades across the x coordinate.
	/// </summary>
	/// <param name="centeredPosition">The x coordinate where the shader is the brightest- darkens when further away from this point. Defaults to 0, the tip of the particle.</param>
	/// <param name="exponent">The exponent to which the shader is raised based on how far away it is from the centered position.</param>
	internal virtual void XDistanceFade(ref float centeredPosition, ref float exponent) { }

	/// <summary>
	/// Controls how the particle tapers from an x coordinate of 1 to an x coordinate of 0.
	/// </summary>
	/// <param name="totalTapering">The amount it's tapered when x is 0. At 1, the particle will be completely pointed, at 0, the particle is rectangular.</param>
	/// <param name="taperExponent">Exponent that affects the easing of the interpolation, controlling the shape of the particle. Curved outwards below 1, curved inwards above 1.</param>
	internal virtual void TaperStyle(ref float totalTapering, ref float taperExponent) { }

	/// <summary>
	/// The final number of colors after posturization. More colors means more of a gradient.
	/// </summary>
	internal virtual int NumColors { get; set; }

	/// <summary>
	/// The rate at which the shader interpolates between the dark color and the light color.
	/// </summary>
	internal virtual float ColorLerpExponent { get; set; }

	/// <summary>
	/// Final modifier to the color after all calculations.
	/// </summary>
	internal virtual float FinalIntensity { get; set; }

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Effect effect = AssetLoader.LoadedShaders["MotionNoiseCone"];
		Texture2D texture = AssetLoader.LoadedTextures["vnoise"].Value;
		effect.Parameters["uTexture"].SetValue(texture);

		Vector2 Tapering = new Vector2(0, 1);
		TaperStyle(ref Tapering.X, ref Tapering.Y);
		effect.Parameters["Tapering"].SetValue(Tapering);

		effect.Parameters["scroll"].SetValue(GetScroll());
		//float dissipation = (float)Math.Max(1.33f * (Progress - 0.25f), 0);

		var dissipation = new Vector3(Progress, 1, 1);
		DissipationStyle(ref dissipation.X, ref dissipation.Y, ref dissipation.Z);
		effect.Parameters["dissipation"].SetValue(dissipation);

		var secondaryColorLerp = Color.Lerp(DarkColor, BrightColor, 1 - dissipation.X);
		effect.Parameters["uColor"].SetValue(DarkColor.ToVector4());
		effect.Parameters["uColor2"].SetValue(secondaryColorLerp.ToVector4());

		var noiseStretch = new Vector2(_length / TextureStretch.X, _width / TextureStretch.Y);
		effect.Parameters["textureStretch"].SetValue(noiseStretch);

		var texExponent = new Vector3(0.01f, 30f, 1);
		TextureExponent(ref texExponent.X, ref texExponent.Y, ref texExponent.Z);
		effect.Parameters["texExponentLerp"].SetValue(texExponent);

		var xDistExponent = new Vector2(0f, 2f);
		XDistanceFade(ref xDistExponent.X, ref xDistExponent.Y);
		effect.Parameters["xDistExponent"].SetValue(xDistExponent);

		effect.Parameters["numColors"].SetValue(NumColors);
		effect.Parameters["colorLerpExponent"].SetValue(ColorLerpExponent);
		effect.Parameters["finalIntensityMod"].SetValue(FinalIntensity);

		Color lightColor = Color.White;
		if (UseLightColor)
			lightColor = Lighting.GetColor(Position.ToTileCoordinates().X, Position.ToTileCoordinates().Y);

		var square = new SquarePrimitive
		{
			Color = lightColor * (1 - dissipation.X),
			Height = _width,
			Length = _length,
			Position = Position - Main.screenPosition,
			Rotation = Rotation + MathHelper.Pi,
		};
		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}
}
