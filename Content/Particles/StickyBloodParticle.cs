using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class StickyBloodParticle : Particle
{
	public bool hitTile;
	public int variant;
	public float fallSpeed;

	public StickyBloodParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, float fallSpeed = 0.15f)
	{
		Position = position;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
		hitTile = false;
		this.fallSpeed = fallSpeed;
		variant = Main.rand.Next(3);
		Rotation = Main.rand.NextFloat(6.28f);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		if (hitTile)
		{
			Velocity *= 0f;
			Velocity.Y += 0.03f;
			return;
		}

		Velocity.Y += fallSpeed;
		Rotation = Velocity.ToRotation();

		Tile tile = Framing.GetTileSafely((int)Position.X / 16, (int)Position.Y / 16);
		if (tile.HasTile && tile.BlockType == BlockType.Solid && Main.tileSolid[tile.TileType] && !hitTile)
		{
			TimeActive++;
			Velocity *= -0.1f;
			hitTile = true;
		}
	}

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
			spriteBatch.Draw(texture, Position + anchorPosition + offset, frame, Color.DarkRed * 0.5f * fade, rotation, frame.Size() / 2, Scale, 0, 0));

		spriteBatch.Draw(texture, Position + anchorPosition, frame, Color.White * fade * 0.75f, rotation, frame.Size() / 2, Scale, 0, 0);
	}
}