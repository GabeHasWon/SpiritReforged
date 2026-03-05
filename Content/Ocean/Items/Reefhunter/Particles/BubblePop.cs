using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;

public class BubblePop : Particle
{
	private const int NUMFRAMES = 8;
	private readonly float _opacity;

	public BubblePop(Vector2 position, float scale, float opacity, int animationTime, float rotation = 0f)
	{
		Position = position;
		Scale = scale;
		_opacity = opacity;
		MaxTime = animationTime;
		Rotation = rotation;
	}

	public override ParticleDrawType DrawType => ParticleDrawType.CustomBatchedAdditiveBlend;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = ParticleHandler.GetTexture(Type);
		var color = Lighting.GetColor(Position.ToTileCoordinates());

		int frameNumber = (int)Math.Floor((double)(Progress * NUMFRAMES));
		var frame = texture.Frame(1, NUMFRAMES, 0, frameNumber, 0, -2);
		var origin = frame.Size() / 2 + new Vector2(0, 5);

		spriteBatch.Draw(texture, Position - Main.screenPosition, frame, color * _opacity, Rotation, origin, Scale, SpriteEffects.None, 0);
	}
}
