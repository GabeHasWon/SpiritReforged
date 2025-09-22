using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.VerletChains;

public class ChainSegment(ChainVertex vertex1, ChainVertex vertex2, float length)
{
	public ChainVertex Vertex1 { get; set; } = vertex1;
	public ChainVertex Vertex2 { get; set; } = vertex2;
	public float Length { get; set; } = length;

	public float Rotation()
	{
		Vector2 delta = Vertex1.Position - Vertex2.Position;
		return (float)Math.Atan2(delta.Y, delta.X);
	}

	public void ConstrainLine()
	{
		Vector2 delta = Vertex2.Position - Vertex1.Position;
		float distance = delta.Length();

		float fraction = (Length - distance) / Math.Max(distance, 1) / 2;

		delta *= fraction;

		if (!Vertex1.Static)
			Vertex1.Position -= delta;
		if (!Vertex2.Static)
			Vertex2.Position += delta;
	}

	public void Draw(SpriteBatch sB)
	{
		Vector2 delta = Vertex1.Position - Vertex2.Position;
		float rotation = (float)Math.Atan2(delta.Y, delta.X);

		sB.Draw(TextureAssets.MagicPixel.Value, Vertex2.Position - Main.screenPosition, null, Color.White, rotation, new Vector2(0f, 0.5f), new Vector2(delta.Length(), 2f), SpriteEffects.None, 0);
	}

	public void Draw(SpriteBatch sB, Texture2D texture, Rectangle source, float scale, Color color = default)
	{
		if (color == default)
			color = Color.White;

		var lightColor = Lighting.GetColor(Vertex2.Position.ToTileCoordinates(), color);
		sB.Draw(texture, Vertex2.Position - Main.screenPosition, source, lightColor, Rotation() + MathHelper.PiOver2, source.Size() / 2f, scale, SpriteEffects.None, 0);
	}
}
