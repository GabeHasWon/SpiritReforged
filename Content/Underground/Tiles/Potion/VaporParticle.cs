using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Underground.Tiles.Potion;

public class VaporParticle : Particle
{
	private const int NumFrames = 8;
	private float _frameCounter;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	public VaporParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, int timeLeft = 60)
	{
		Position = position;
		Velocity = velocity;
		Color = color;
		Scale = scale;
		MaxTime = timeLeft;

		_frameCounter = Main.rand.NextFloat(NumFrames);
	}

	public override void Update()
	{
		_frameCounter = (_frameCounter + 0.2f) % NumFrames;
		Velocity *= 0.95f;
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		float easeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);
		float scale = Scale * (0.5f + Progress * 0.5f);

		var color = Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGBA(Color) * easeModifier;
		var source = Texture.Frame(1, NumFrames, 0, (int)_frameCounter, 0);

		spriteBatch.Draw(Texture, Position - Main.screenPosition, source, color, Rotation, source.Size() / 2, scale, SpriteEffects.None, 0);
	}
}