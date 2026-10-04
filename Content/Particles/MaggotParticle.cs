using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class MaggotParticle : Particle
{
	private readonly int _variant;

	public MaggotParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime)
	{
		Position = position;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		_variant = Main.rand.Next(3);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		Velocity *= 0.99f;
		Velocity.Y += 0.05f;

		Rotation += Velocity.Length() * 0.05f * Math.Sign(Velocity.X);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Texture2D bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;
		
		Rectangle source = texture.Frame(1, 3, 0, _variant);
		float fade = 1f - Progress;
		Vector2 anchorPosition = settings.AnchorPosition;

		DrawHelpers.DrawOutline(offset =>
			spriteBatch.Draw(texture, Position + anchorPosition + offset, source, Color.Black * fade * 0.3f, rotation, source.Size() / 2, Scale, 0, 0));

		spriteBatch.Draw(texture, Position + anchorPosition, source, Lighting.GetColor(Position.ToTileCoordinates()) * fade, rotation, source.Size() / 2, Scale, 0, 0);
	}
}
