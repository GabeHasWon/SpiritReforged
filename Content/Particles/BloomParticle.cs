using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;
public class BloomParticle : GlowParticle
{
	public BloomParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float scale, int maxTime, int maxTrailLength = 1, Action<Particle> extraUpdateAction = null) : base(position, velocity, startColor, endColor, scale, maxTime, maxTrailLength, extraUpdateAction) { }
	public BloomParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, int maxTrailLength = 1, Action<Particle> extraUpdateAction = null) : this(position, velocity, color, color, scale, maxTime, maxTrailLength, extraUpdateAction) { }

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D tex = ParticleHandler.GetTexture(Type);
		Texture2D bloom = AssetLoader.LoadedTextures["BloomHarsh"].Value;
		float scaleTimeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;

			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			Draw(bloom, oldPositions[i], easeModifier * 0.2f, easeModifier * 0.15f);
		}

		for (int i = 0; i < oldPositions.Length; i++)
		{
			float progress = i / (float)oldPositions.Length;

			float easeModifier = EaseFunction.EaseQuadOut.Ease(1 - progress);
			Draw(tex, oldPositions[i], easeModifier * 0.25f, easeModifier);
		}

		void Draw(Texture2D drawTex, Vector2 pos, float opacity, float scaleMod)
		{
			spriteBatch.Draw(drawTex, pos - Main.screenPosition, null, Color.Additive() * opacity * 0.15f, 0, drawTex.Size() / 2, scaleMod * Scale * scaleTimeModifier * 2f, SpriteEffects.None, 0);

			spriteBatch.Draw(drawTex, pos - Main.screenPosition, null, Color.Additive() * opacity * 0.33f, 0, drawTex.Size() / 2, scaleMod * Scale * scaleTimeModifier, SpriteEffects.None, 0);
		}
	}
}
