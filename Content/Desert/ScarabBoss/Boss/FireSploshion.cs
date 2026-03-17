using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

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
		Color = Color.White;
		style = Main.rand.Next(2);
	}

	public override ParticleLayer DrawLayer => ParticleLayer.BelowSolid;
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D tex = Texture;
		Rectangle source = Texture.Frame(2, 7, style, (int)((float)TimeActive / MaxTime * 6));

		spriteBatch.Draw(tex, Position - Main.screenPosition, source, Color, 0, source.Size() / 2, Scale, 0, 0);
		spriteBatch.Draw(tex, Position - Main.screenPosition, source, (Color * 0.5f).Additive(), 0, source.Size() / 2, Scale * 1.1f, 0, 0);
	}
}