using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class BeeParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime) : FlyParticle(position, velocity, rotation, scale, maxTime)
{
	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		Velocity *= 0.96f;
		Velocity = Velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.9f, 1.1f);

		if (Main.rand.NextBool(60))
			Velocity += Vector2.One.RotatedBy(6.28f / Main.rand.Next(1, 4));
	}
}

public class LargeBeeParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime) : BeeParticle(position, velocity, rotation, scale, maxTime)
{
	public const int FRAME_COUNT = 4;

	private int _frame;
	private int _frameCounter;

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		if (++_frameCounter > 3)
		{
			_frameCounter = 0;

			if (++_frame >= FRAME_COUNT)
				_frame = 0;
		}
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Main.instance.LoadProjectile(ProjectileID.Bee);

		Texture2D texture = TextureAssets.Projectile[ProjectileID.Bee].Value;
		float rotation = Rotation;
		float fade;

		if (Progress < 0.5f)
			fade = Progress / 0.5f;
		else
			fade = 1f - (Progress - 0.5f) / 0.5f;

		Rectangle frame = texture.Frame(1, FRAME_COUNT, frameY: _frame);
		Vector2 anchorPosition = settings.AnchorPosition;
		SpriteEffects flip = Velocity.X < 0 ? SpriteEffects.FlipHorizontally : 0f;

		DrawHelpers.DrawOutline(offset =>
			spriteBatch.Draw(texture, Position + anchorPosition + offset, frame, Color.Black * fade * 0.3f, rotation, frame.Size() / 2, Scale, 0, 0));

		spriteBatch.Draw(texture, Position + anchorPosition, frame, Lighting.GetColor(Position.ToTileCoordinates()) * fade, rotation, frame.Size() / 2, Scale, flip, 0);
	}
}