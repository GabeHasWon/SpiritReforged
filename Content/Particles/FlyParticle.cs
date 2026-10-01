using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class FlyParticle : Particle
{
	public FlyParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime)
	{
		Position = position;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		Velocity *= 0.99f;
		Velocity = Velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.9f, 1.1f);

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;
		Vector2 anchorPosition = settings.AnchorPosition;
		float fade;
		
		if (Progress < 0.5f)
			fade = Progress / 0.5f;
		else
			fade = 1f - (Progress - 0.5f) / 0.5f;

		DrawHelpers.DrawOutline(offset =>
			spriteBatch.Draw(texture, Position + anchorPosition + offset, null, Color.Black * fade * 0.3f, rotation, texture.Size() / 2, Scale, 0, 0));

		spriteBatch.Draw(texture, Position + anchorPosition, null, Lighting.GetColor(Position.ToTileCoordinates()) * fade, rotation, texture.Size() / 2, Scale, 0, 0);
	}
}