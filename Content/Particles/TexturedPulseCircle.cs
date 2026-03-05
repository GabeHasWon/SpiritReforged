using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class TexturedPulseCircle : PulseCircle
{
	private readonly Texture2D _texture;
	private readonly Vector2 _textureStretch;

	public TexturedPulseCircle(Vector2 position, Color ringColor, Color bloomColor, float ringWidth, float maxRadius, int maxTime, string texture, Vector2 textureStretch, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : base(position, ringColor, bloomColor, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth)
	{
		_texture = AssetLoader.LoadedTextures[texture].Value;
		_textureStretch = textureStretch;
	}

	public TexturedPulseCircle(Vector2 position, Color ringColor, Color bloomColor, float ringWidth, float maxRadius, int maxTime, Texture2D texture, Vector2 textureStretch, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : base(position, ringColor, bloomColor, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth)
	{
		_texture = texture;
		_textureStretch = textureStretch;
	}

	public TexturedPulseCircle(Vector2 position, Color color, float ringWidth, float maxRadius, int maxTime, string texture, Vector2 textureStretch, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : base(position, color, color * 0.25f, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth)
	{
		_texture = AssetLoader.LoadedTextures[texture].Value;
		_textureStretch = textureStretch;
	}

	public TexturedPulseCircle(Vector2 position, Color color, float ringWidth, float maxRadius, int maxTime, Texture2D texture, Vector2 textureStretch, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : base(position, color, color * 0.25f, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth)
	{
		_texture = texture;
		_textureStretch = textureStretch;
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	internal override string EffectPassName => "TexturedStyle";

	internal override void EffectExtras(ref Effect curEffect)
	{
		curEffect.Parameters["uTexture"].SetValue(_texture);
		curEffect.Parameters["textureStretch"].SetValue(new Vector2(_textureStretch.X, _textureStretch.Y));
		curEffect.Parameters["scroll"].SetValue(Progress / 3);
	}
}
