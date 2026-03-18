using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class CartoonHit : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public readonly int style;

	public CartoonHit(Vector2 position, int duration, float scale = 1, float rotation = 0)
	{
		Position = position;
		Scale = scale;
		Rotation = rotation;
		MaxTime = duration;

		Color = Color.White;
		Velocity = new Vector2(-2).RotatedBy(rotation);
		style = Main.rand.Next(3);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 3, 0, style, 0, -2);

		spriteBatch.Draw(texture, Position - Main.screenPosition + new Vector2(2).RotatedBy(Rotation), source, Color.White * 0.5f, Rotation, source.Size() / 2, Scale, 0, 0);
		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color.White, Rotation, source.Size() / 2, Scale, 0, 0);
	}
}
