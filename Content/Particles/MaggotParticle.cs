using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class MaggotParticle : Particle
{
	private int variant;
	public MaggotParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime)
	{
		Position = position;
		Color = Color.White;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		variant = Main.rand.Next(3);
	}

	public override void Update()
	{
		Velocity *= 0.99f;
		Velocity.Y += 0.05f;

		Rotation += Velocity.Length() * 0.05f * Math.Sign(Velocity.X);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;
		
		Rectangle source = texture.Frame(1, 3, 0, variant);

		float fade = 1f - Progress;

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.Black * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.5f, 0, 0);

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color * fade, rotation, source.Size() / 2, Scale, 0, 0);
	}

	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowNPC;
	public override ParticleLayer DrawLayer => Layer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
}
