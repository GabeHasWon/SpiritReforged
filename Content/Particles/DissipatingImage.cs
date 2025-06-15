using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Particles;

public class DissipatingImage : Particle
{
	public Color? SecondaryColor { get; set; } = null;
	public Color? TertiaryColor { get; set; } = null;

	public ParticleLayer Layer { get; set; } = ParticleLayer.AboveProjectile;

	public float ColorLerpExponent { get; set; } = 1;
	public float Intensity { get; set; } = 1;
	public float DissolveAmount { get; set; } = 0;

	public bool UseLightColor { get; set; }

	private readonly string _texture;
	private readonly float _maxDistortion;
	private readonly Vector2 _noiseStretch = new (1);
	private readonly Vector2 _texExponent = new(2, 1);

	private float _opacity;

	internal float _scaleMod = 1;

	public DissipatingImage(Vector2 position, Color color, float rotation, float scale, float maxDistortion, string texture, int maxTime)
	{
		Position = position;
		Rotation = rotation;
		Scale = scale;
		_texture = texture;
		_maxDistortion = maxDistortion;
		Color = color;
		MaxTime = maxTime;
	}
	public DissipatingImage(Vector2 position, Color color, float rotation, float scale, float maxDistortion, string texture, Vector2 noiseScale, Vector2 textureExponentRange, int maxTime) : this(position, color, rotation, scale, maxDistortion, texture, maxTime)
	{
		_noiseStretch = noiseScale;
		_texExponent = textureExponentRange;
	}

	public DissipatingImage UsesLightColor()
	{
		UseLightColor = true;
		return this;
	}

	public override void Update()
	{
		_opacity = EaseFunction.EaseQuadOut.Ease(Progress);
		_opacity = (float)Math.Sin(_opacity * MathHelper.Pi);
		_scaleMod = 1 + Progress / 2;
	}

	public override ParticleLayer DrawLayer => Layer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		if (!AssetLoader.LoadedTextures.TryGetValue(_texture, out Asset<Texture2D> asset))
			throw new ArgumentNullException(_texture, "Given input does not correspond to a loaded asset.");

		else
		{
			Effect effect = AssetLoader.LoadedShaders["DistortDissipateTexture"];
			effect.Parameters["primaryColor"].SetValue(Color.ToVector4());
			effect.Parameters["secondaryColor"].SetValue((SecondaryColor ?? Color).ToVector4());
			effect.Parameters["tertiaryColor"].SetValue((TertiaryColor ?? Color).ToVector4());
			effect.Parameters["colorLerpExp"].SetValue(ColorLerpExponent);

			effect.Parameters["Progress"].SetValue(Progress);
			effect.Parameters["uTexture"].SetValue(asset.Value);
			effect.Parameters["noise"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
			effect.Parameters["secondaryNoise"].SetValue(AssetLoader.LoadedTextures["fbmNoise"].Value);
			effect.Parameters["coordMods"].SetValue(_noiseStretch);
			effect.Parameters["intensity"].SetValue(EaseFunction.EaseCubicOut.Ease(1 - Progress) * Intensity);

			effect.Parameters["distortion"].SetValue(_maxDistortion * EaseFunction.EaseQuadOut.Ease(Progress));
			effect.Parameters["dissolve"].SetValue(EaseFunction.EaseCubicInOut.Ease(Progress) * DissolveAmount);

			float texExponent = MathHelper.Lerp(_texExponent.X, _texExponent.Y, _opacity);
			effect.Parameters["texExponent"].SetValue(texExponent);

			Color lightColor = Color.White;
			if (UseLightColor)
				lightColor = Lighting.GetColor(Position.ToTileCoordinates().X, Position.ToTileCoordinates().Y);

			var square = new SquarePrimitive
			{
				Color = lightColor,
				Height = Scale * asset.Height() * _scaleMod,
				Length = Scale * asset.Width() * _scaleMod,
				Position = Position - Main.screenPosition,
				Rotation = Rotation,
			};
			PrimitiveRenderer.DrawPrimitiveShape(square, effect);
		}
	}
}
