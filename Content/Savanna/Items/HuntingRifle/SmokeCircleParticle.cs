using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;

namespace SpiritReforged.Content.Savanna.Items.HuntingRifle;

public class SmokeCircleParticle : Particle
{
	private readonly int _maxTime;

	private readonly float _secondaryRotation;

	private readonly Vector2 _scrollOffset;
	private readonly Vector2 _noiseScale;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public SmokeCircleParticle(Vector2 position, Vector2 velocity, Color color, float scale, float rotation, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Color = color;
		Scale = scale;
		Rotation = rotation;
		_maxTime = maxTime;

		_secondaryRotation = rotation + Main.rand.NextFloat(-0.2f, 0.2f);
		_scrollOffset = new(Main.rand.NextFloat(), Main.rand.NextFloat());
		_noiseScale = new(Main.rand.NextFloat(0.2f, 0.3f), Main.rand.NextFloat(0.2f, 0.3f));
	}

	public override void Update()
	{
		Scale += 1f / _maxTime * .3f;

		if (TimeActive > _maxTime)
			Kill();
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var tex = TextureAssets.GlowMask[239].Value;
		float progress = TimeActive / (float)_maxTime;
		var size = new Vector2(.25f * tex.Width, 1f * tex.Height) * Scale;

		Effect effect = AssetLoader.LoadedShaders["DistortDissipateTexture"].Value;
		Color baseColor = ((TimeActive < 3) ? Color * 1.5f : Color);
		effect.Parameters["primaryColor"].SetValue(baseColor.ToVector4());
		effect.Parameters["secondaryColor"].SetValue(baseColor.ToVector4() / 1.5f);
		effect.Parameters["tertiaryColor"].SetValue(baseColor.ToVector4() / 2);
		effect.Parameters["colorLerpExp"].SetValue(1);
		effect.Parameters["intensity"].SetValue(1f);

		effect.Parameters["uTexture"].SetValue(tex);
		effect.Parameters["texExponent"].SetValue(.25f);

		effect.Parameters["noise"].SetValue(TextureAssets.Extra[193].Value);
		effect.Parameters["secondaryNoise"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["scroll"].SetValue(_scrollOffset);
		effect.Parameters["coordMods"].SetValue(_noiseScale);

		effect.Parameters["Progress"].SetValue(progress);
		effect.Parameters["distortion"].SetValue(EaseFunction.EaseQuadInOut.Ease(progress) * 0.33f);
		effect.Parameters["dissolve"].SetValue(EaseFunction.EaseCubicOut.Ease(progress));
		effect.Parameters["pixellate"].SetValue(true);
		effect.Parameters["pixelDimensions"].SetValue(size);

		var squares = new List<SquarePrimitive>()
		{
			new() {
				Color = Color.White * (1f - progress) * 0.7f,
				Height = size.Y,
				Length = size.X,
				Position = Position - Main.screenPosition,
				Rotation = Rotation,
			},

			new() {
				Color = Color.White * (1f - progress),
				Height = size.Y * 0.9f,
				Length = size.X * 0.9f,
				Position = Position - Main.screenPosition,
				Rotation = MathHelper.Lerp(Rotation, _secondaryRotation, 0.5f),
			},

			new() {
				Color = Color.White * (1f - progress) * 0.7f,
				Height = size.Y * 0.8f,
				Length = size.X * 0.8f,
				Position = Position - Main.screenPosition,
				Rotation = _secondaryRotation,
			},
		};

		Common.PrimitiveRendering.PrimitiveRenderer.DrawPrimitiveShapeBatched(squares.ToArray(), effect);
	}
}
