using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Forest.Safekeeper;

public class HolyStar : Particle
{
	public HolyStar(Vector2 position, Color color, float scale, int maxTime)
	{
		Position = position;
		Color = color.Additive();
		Scale = scale;
		MaxTime = maxTime;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		var basetexture = Texture;
		Vector2 randomOffset = (Vector2.UnitX * (float)Math.Sin(Main.timeForVisualEffects / 20f) * 2f).RotatedBy(Main.timeForVisualEffects / 50f);

		for (int i = 0; i < 3; i++)
		{
			var c = ((i == 0) ? (Color.Magenta * 0.5f) : ((i == 1) ? (Color.Blue * 0.5f) : Color)).Additive() * (1f - Progress);
			var p = Position + randomOffset * ((i == 0) ? 1 : ((i == 1) ? -1 : 0)) + settings.AnchorPosition;
			float s = (1f - Progress) * Scale;

			spriteBatch.Draw(basetexture, p, null, c * 0.5f, Rotation * 1.5f, basetexture.Size() / 2, s * 0.75f, SpriteEffects.None, 0);
			spriteBatch.Draw(basetexture, p, null, c * 0.5f, -Rotation * 1.5f, basetexture.Size() / 2, s * 0.75f, SpriteEffects.None, 0);
			spriteBatch.Draw(basetexture, p, null, c, Rotation, basetexture.Size() / 2, s, SpriteEffects.None, 0);
		}
	}
}