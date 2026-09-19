using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class ShimmerStar : Particle
{
	private readonly Color _color;

	public ShimmerStar(Vector2 position, Color color, float scale, int maxTime, Vector2 velocity = default)
	{
		LocalPosition = position;
		_color = color.Additive();
		Scale = new Vector2(scale);
		TimeMax = maxTime;
		Velocity = velocity;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		Rotation += 0.01f;
		Velocity *= 0.98f;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture;
		Color color = _color * Progress;
		Vector2 scale = Scale * (1f - Progress);

		spritebatch.Draw(texture, LocalPosition + settings.AnchorPosition, null, color, Rotation, texture.Size() / 2, scale, 0, 0);
	}
}