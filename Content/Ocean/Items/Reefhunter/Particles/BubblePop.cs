using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;

public class BubblePop : Particle
{
	private const int NUMFRAMES = 8;

	public BubblePop(Vector2 position, float scale, float opacity, int animationTime, float rotation = 0f)
	{
		Position = position;
		Scale = scale;
		Color = Color.White.Additive() * opacity;
		MaxTime = animationTime;
		Rotation = rotation;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Color color = Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGBA(Color);

		int frameNumber = (int)Math.Floor((double)(Progress * NUMFRAMES));
		Rectangle source = texture.Frame(1, NUMFRAMES, 0, frameNumber, 0, -2);
		Vector2 origin = source.Size() / 2 + new Vector2(0, 5);

		spriteBatch.Draw(texture, Position + settings.AnchorPosition, source, color, Rotation, origin, Scale, SpriteEffects.None, 0);
	}
}