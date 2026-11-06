using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class CartoonSmoke : Particle
{
	public CartoonSmoke(Vector2 position, int duration, float scale = 1, Vector2? velocity = null)
	{
		Position = position;
		Scale = scale;
		MaxTime = duration;
		Velocity = velocity ?? Vector2.Zero;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);
		Rotation += 0.01f;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 6, 0, (int)(Progress * 6), 0, -2);

		float rotation = Rotation - MathHelper.PiOver2;
		spriteBatch.Draw(texture, Position + settings.AnchorPosition, source, Lighting.GetColor(Position.ToTileCoordinates()), rotation, source.Size() / 2, Scale, 0, 0);
	}
}