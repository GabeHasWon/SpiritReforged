using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Forest.Candles;

public class GhostFlameParticle : ABasicParticle
{
	public const int FRAME_COUNT = 5;
	public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<GhostFlameParticle>("GhostFlameParticle", false);

	public int TimeLeft = 20;

	protected int _timeActive;
	protected float _opacity;

	public override void Update(ref ParticleRendererSettings settings)
	{
		Rotation = Velocity.ToRotation();

		if (++_timeActive >= TimeLeft)
			ShouldBeRemovedFromRenderer = true;

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture.Value;

		float progress = _timeActive / (float)TimeLeft;
		int frame = (int)(progress * FRAME_COUNT);
		Rectangle source = texture.Frame(1, FRAME_COUNT, 0, frame, 0, -2);
		Vector2 position = LocalPosition + settings.AnchorPosition;

		DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
			spritebatch.Draw(texture, position + offset, source, Color.DarkGray.Additive(100) * 0.1f, Rotation, source.Size() / 2, Scale, 0, 0));

		spritebatch.Draw(texture, position, source, Color.Lerp(Color.White, Color.DarkGray, progress), Rotation, source.Size() / 2, Scale, 0, 0);
	}
}