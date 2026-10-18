using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class StickyHoneyParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, float fallSpeed = 0.15f) : StickyBloodParticle(position, velocity, scale, maxTime, fallSpeed)
{
	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		float rotation = Rotation;
		float fade;

		Vector2 anchorPosition = settings.AnchorPosition;
		Rectangle frame = texture.Frame(1, 3, 0, variant);

		if (Progress < 0.25f)
			fade = Progress / 0.25f;
		else
			fade = 1f - (Progress - 0.25f) / 0.75f;

		DrawHelpers.DrawOutline(offset =>
			spriteBatch.Draw(texture, Position + anchorPosition + offset, frame, Color.Orange * 0.5f * fade, rotation, frame.Size() / 2, Scale, 0, 0));

		spriteBatch.Draw(texture, Position + anchorPosition, frame, Color.White * fade, rotation, frame.Size() / 2, Scale, 0, 0);
	}
}