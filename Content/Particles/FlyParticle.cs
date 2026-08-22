using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class FlyParticle : Particle
{
	public FlyParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime)
	{
		Position = position;
		Color = Color.White;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
	}

	public override void Update()
	{
		Velocity *= 0.99f;
		Velocity = Velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.9f, 1.1f);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;

		float fade;
		
		if (Progress < 0.5f)
			fade = (Progress / 0.5f);
		else
			fade = (1f - (Progress - 0.5f) / 0.5f);

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		
		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.Black * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.2f, 0, 0);
		
		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * fade, rotation, texture.Size() / 2, Scale, 0, 0);
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
}
