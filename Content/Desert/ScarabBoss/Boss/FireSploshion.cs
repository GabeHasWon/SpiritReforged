using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class FireSploshion : Particle
{
	public readonly int style;

	public FireSploshion(Vector2 worldPosition, int duration, float scale = 1) : base()
	{
		Position = worldPosition;
		MaxTime = duration;
		Scale = scale;
		Rotation = Main.rand.NextFloat(MathHelper.PiOver2);
		style = Main.rand.Next(2);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D tex = Texture;
		Rectangle source = Texture.Frame(2, 7, style, (int)(Progress * 6));

		spriteBatch.Draw(tex, Position + settings.AnchorPosition, source, Color.White, 0, source.Size() / 2, Scale, 0, 0);
		spriteBatch.Draw(tex, Position + settings.AnchorPosition - Main.screenPosition, source, (Color.White * 0.5f).Additive(), 0, source.Size() / 2, Scale * 1.1f, 0, 0);
	}
}