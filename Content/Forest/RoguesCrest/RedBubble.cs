using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Forest.RoguesCrest;

public class RedBubble : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public RedBubble(Vector2 position, Color color, float scale, int maxTime)
	{
		Position = position;
		Color = color;
		Scale = scale;
		MaxTime = maxTime;
	}

	public override void Update() { }

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = ParticleHandler.GetTexture(Type);
		var source = texture.Frame(1, 5, 0, (int)(Progress * 5), 0, -2);
		var c = Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGB(Color);

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, c, Rotation * 1.5f, source.Size() / 2, Scale, SpriteEffects.None, 0);
	}
}