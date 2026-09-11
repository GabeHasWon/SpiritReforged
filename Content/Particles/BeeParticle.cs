using SpiritReforged.Common.Misc;
using System.Reflection.Metadata;

namespace SpiritReforged.Content.Particles;
public class BeeParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime) : FlyParticle(position, velocity, rotation, scale, maxTime)
{
	public override void Update()
	{
		Velocity *= 0.96f;
		Velocity = Velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.9f, 1.1f);
		if (Main.rand.NextBool(60))
			Velocity += Vector2.One.RotatedBy(6.28f / Main.rand.Next(1, 4));
	}
}

public class LargeBeeParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime) : BeeParticle(position, velocity, rotation, scale, maxTime)
{
	internal const int FRAME_COUNT = 4;

	internal int _frame;
	internal int _frameCounter;

	public override void Update()
	{
		base.Update();

		if (++_frameCounter > 3)
		{
			_frameCounter = 0;
			_frame++;

			if (_frame >= FRAME_COUNT)
				_frame = 0;
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Main.instance.LoadProjectile(ProjectileID.Bee);

		var texture = TextureAssets.Projectile[ProjectileID.Bee].Value;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;

		float fade;

		if (Progress < 0.5f)
			fade = (Progress / 0.5f);
		else
			fade = (1f - (Progress - 0.5f) / 0.5f);

		Rectangle frame = texture.Frame(1, FRAME_COUNT, frameY: _frame);

		SpriteEffects flip = Velocity.X < 0 ? SpriteEffects.FlipHorizontally : 0f;

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.Black * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.5f, 0, 0);

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * fade, rotation, frame.Size() / 2, Scale, flip, 0);
	}
}
