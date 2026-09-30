using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Forest.RoguesCrest;

public class RedBubble : Particle
{
	public Color Color { get; protected set; }

	public RedBubble(Vector2 position, Color color, float scale, int maxTime)
	{
		Position = position;
		Color = color;
		Scale = scale;
		MaxTime = maxTime;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var source = texture.Frame(1, 5, 0, (int)(Progress * 5), 0, -2);
		var c = Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGB(Color);

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, c, Rotation * 1.5f, source.Size() / 2, Scale, SpriteEffects.None, 0);
	}
}