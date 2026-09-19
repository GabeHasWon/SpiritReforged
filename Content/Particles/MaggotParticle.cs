using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class MaggotParticle : Particle
{
	private readonly int _variant;
	private readonly Color _tint;

	public MaggotParticle(Vector2 position, Vector2 velocity, float rotation, float scale, int maxTime)
	{
		LocalPosition = position;
		_tint = Color.White;
		Rotation = rotation;
		Scale = new Vector2(scale);
		TimeMax = maxTime;
		Velocity = velocity;

		_variant = Main.rand.Next(3);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
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

		spriteBatch.End(); //BATCH ME!!!
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		spriteBatch.Draw(bloom, LocalPosition + settings.AnchorPosition, null, Color.Black * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.5f, 0, 0);

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, LocalPosition + settings.AnchorPosition, source, _tint * fade, rotation, source.Size() / 2, Scale, 0, 0);
	}
}
