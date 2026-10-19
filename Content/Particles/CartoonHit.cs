using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class CartoonHit : Particle
{
	public readonly int style;

	public CartoonHit(Vector2 position, int duration, float scale = 1, float rotation = 0, Vector2? velocity = null)
	{
		Position = position;
		Scale = scale;
		Rotation = rotation;
		MaxTime = duration;
		Velocity = velocity ?? new Vector2(-2).RotatedBy(rotation);
		style = Main.rand.Next(3);
	}

	public override void Draw(ref ParticleRendererSettings settings,SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 3, 0, style, 0, -2);

		float rotation = Rotation - MathHelper.PiOver2;

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color.White, rotation, source.Size() / 2, Scale * (1f - Progress), 0, 0);
	}
}
