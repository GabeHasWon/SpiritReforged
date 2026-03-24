using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class RoarRing(Vector2 position, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : PulseCircle(position, Color.White, Color.White, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth)
{
	private readonly Texture2D _texture = AssetLoader.LoadedTextures["noise"].Value;
	private readonly Vector2 _textureStretch = new(3, 0.25f);

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	internal override string EffectPassName => "RoarStyle";

	internal override void EffectExtras(ref Effect curEffect)
	{
		curEffect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
		curEffect.Parameters["textureStretch"].SetValue(new Vector2(_textureStretch.X * 0.75f, _textureStretch.Y / 4));
		curEffect.Parameters["scroll"].SetValue(0);
	}
}
