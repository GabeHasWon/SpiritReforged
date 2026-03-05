using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class SteamParticle : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public override ParticleLayer DrawLayer => _layer;

	private readonly ParticleLayer _layer;

	public SteamParticle(Vector2 position, Vector2 velocity, float scale, int timeLeft = 60, ParticleLayer layer = ParticleLayer.BelowSolid)
	{
		Position = position;
		Velocity = velocity;
		Scale = scale;
		MaxTime = timeLeft;
		_layer = layer;
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		float easeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);
		float fadeIn = 1f - EaseFunction.EaseCubicIn.Ease(1 - Progress);

		float scale = Scale * easeModifier;
		var color = Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGBA(Color) * fadeIn;

		spriteBatch.Draw(Texture, Position - Main.screenPosition, null, color, 0, Texture.Size() / 2, scale, SpriteEffects.None, 0);
	}
}
