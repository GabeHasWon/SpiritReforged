using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class DissipatingImage : Particle
{
	public Color? SecondaryColor { get; set; } = null;
	public Color? TertiaryColor { get; set; } = null;

	public virtual bool UseLightColor { get; set; }
	public virtual bool Pixellate { get; set; }

	public virtual string DistortNoiseString => "noise";

	public virtual float ColorLerpExponent { get; set; } = 1;
	public virtual float Intensity { get; set; } = 1;
	public virtual float DissolveAmount { get; set; } = 0;
	public virtual float FinalScaleMod { get; set; } = 1.5f;
	public virtual float PixelDivisor { get; set; } = 1.5f;

	public virtual EaseFunction DistortEasing { get; set; } = EaseFunction.EaseQuadIn;

	new private readonly Texture2D _texture;
	private readonly float _maxDistortion;
	private readonly Color _tint;
	private readonly Vector2 _noiseStretch = new (1);
	private readonly Vector2 _texExponent = new(2, 1);
	private readonly Vector2 _scrollOffset = new(Main.rand.NextFloat(), Main.rand.NextFloat());

	protected float _opacity;
	protected float _scaleMod = 1;

	public DissipatingImage(Vector2 position, Color color, float rotation, float scale, float maxDistortion, string texture, int maxTime)
	{
		LocalPosition = position;
		Rotation = rotation;
		Scale = new Vector2(scale);
		_texture = AssetLoader.LoadedTextures[texture].Value;
		_maxDistortion = maxDistortion;
		_tint = color;
		TimeMax = maxTime;
	}

	public DissipatingImage(Vector2 position, Color color, float rotation, float scale, float maxDistortion, string texture, Vector2 noiseScale, Vector2 textureExponentRange, int maxTime) : this(position, color, rotation, scale, maxDistortion, texture, maxTime)
	{
		_noiseStretch = noiseScale;
		_texExponent = textureExponentRange;
	}

	public DissipatingImage(Vector2 position, Color color, float rotation, float scale, float maxDistortion, Texture2D texture, int maxTime)
	{
		LocalPosition = position;
		Rotation = rotation;
		Scale = new Vector2(scale);
		_texture = texture;
		_maxDistortion = maxDistortion;
		_tint = color;
		TimeMax = maxTime;
	}

	public DissipatingImage(Vector2 position, Color color, float rotation, float scale, float maxDistortion, Texture2D texture, Vector2 noiseScale, Vector2 textureExponentRange, int maxTime) : this(position, color, rotation, scale, maxDistortion, texture, maxTime)
	{
		_noiseStretch = noiseScale;
		_texExponent = textureExponentRange;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		_opacity = EaseFunction.EaseQuadOut.Ease(Progress);
		_opacity = (float)Math.Sin(_opacity * MathHelper.Pi);
		_scaleMod = MathHelper.Lerp(1, FinalScaleMod, Progress);
	}

	public virtual Color GetLightColor() => UseLightColor ? Lighting.GetColor(LocalPosition.ToTileCoordinates()) : Color.White;

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Effect effect = AssetLoader.LoadedShaders["DistortDissipateTexture"].Value;
		Vector2 size = Scale * _texture.Size() * _scaleMod;

		effect.Parameters["primaryColor"].SetValue(_tint.ToVector4());
		effect.Parameters["secondaryColor"].SetValue((SecondaryColor ?? _tint).ToVector4());
		effect.Parameters["tertiaryColor"].SetValue((TertiaryColor ?? _tint).ToVector4());
		effect.Parameters["colorLerpExp"].SetValue(ColorLerpExponent);

		effect.Parameters["Progress"].SetValue(Progress);
		effect.Parameters["uTexture"].SetValue(_texture);
		effect.Parameters["noise"].SetValue(AssetLoader.LoadedTextures[DistortNoiseString].Value);
		effect.Parameters["secondaryNoise"].SetValue(AssetLoader.LoadedTextures["fbmNoise"].Value);
		effect.Parameters["coordMods"].SetValue(_noiseStretch);
		effect.Parameters["scroll"].SetValue(_scrollOffset);
		effect.Parameters["intensity"].SetValue(Intensity * MathHelper.Lerp(_opacity, 1, DissolveAmount));

		effect.Parameters["distortion"].SetValue(_maxDistortion * DistortEasing.Ease(Progress));
		effect.Parameters["dissolve"].SetValue(EaseFunction.EaseCubicInOut.Ease(Progress) * DissolveAmount);
		effect.Parameters["doDissolve"].SetValue(DissolveAmount > 0);

		effect.Parameters["pixellate"].SetValue(Pixellate);
		effect.Parameters["pixelDimensions"].SetValue(size / PixelDivisor);

		float texExponent = MathHelper.Lerp(_texExponent.X, _texExponent.Y, _opacity);
		effect.Parameters["texExponent"].SetValue(texExponent);

		SquarePrimitive square = new()
		{
			Color = GetLightColor(),
			Height = size.Y,
			Length = size.X,
			Position = LocalPosition + settings.AnchorPosition,
			Rotation = Rotation,
		};
		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}
}
