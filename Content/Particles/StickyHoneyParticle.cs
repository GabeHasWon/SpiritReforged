using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Particles;
public class StickyHoneyParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, float fallSpeed = 0.15f) : StickyBloodParticle(position, velocity, scale, maxTime, fallSpeed)
{
	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;

		float fade;

		Rectangle frame = texture.Frame(1, 3, 0, variant);

		if (Progress < 0.25f)
			fade = Progress / 0.25f;
		else
			fade = (1f - (Progress - 0.25f) / 0.75f);

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.Orange * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.35f, 0, 0);

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * fade, rotation, frame.Size() / 2, Scale, 0, 0);
	}
}
