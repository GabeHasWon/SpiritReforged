using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Tiles.Mirror;

public class EnchantedMirrorShard : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	private readonly int _variant;

	public EnchantedMirrorShard(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime)
	{
		Position = position;
		Color = Color.White;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		_variant = Main.rand.Next(4);
	}

	public override void Update()
	{
		if (Collision.SolidCollision(Position - new Vector2(4), 8, 8))
		{
			SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Pitch = 1, PitchVariance = 0.5f }, Position);
			Kill();
		}
		else
		{
			Velocity *= 0.99f;
			Velocity.Y += 0.25f;
		}

		Scale = 1f - Progress;
		Rotation += Velocity.Length() * 0.05f * Math.Sign(Velocity.X);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Rectangle source = Texture.Frame(1, 4, 0, _variant);

		DrawHelpers.DrawOutline(spriteBatch, default, default, default, (offset) =>
			spriteBatch.Draw(Texture, Position - Main.screenPosition + offset, source, Color.Cyan.Additive() * Progress * 1.5f, Rotation, source.Size() / 2, Scale, 0, 0));

		spriteBatch.Draw(Texture, Position - Main.screenPosition, source, Color, Rotation, source.Size() / 2, Scale, 0, 0);
	}
}
