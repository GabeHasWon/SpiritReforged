using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class RoarRing(Vector2 position, float ringWidth, float maxRadius, int maxTime, EaseFunction MovementStyle = null, bool inverted = false, float endRingWidth = 0) : PulseCircle(position, Color.White, Color.White, ringWidth, maxRadius, maxTime, MovementStyle, inverted, endRingWidth)
{
	public Texture2D NoiseTexture { get; set; } = AssetLoader.LoadedTextures["vnoise"].Value;
	public Vector2 TextureStretch { get; set; } = new(2.25f, 0.065f);
	public float Opacity { get; set; } = 1;

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	internal override string EffectPassName => "RoarStyle";

	internal override void EffectExtras(ref Effect curEffect)
	{
		curEffect.Parameters["uTexture"].SetValue(NoiseTexture);
		curEffect.Parameters["textureStretch"].SetValue(TextureStretch);
		curEffect.Parameters["scroll"].SetValue(0);
		curEffect.Parameters["RingColor"].SetValue(Color.ToVector4() * Opacity);
	}
}
