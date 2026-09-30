using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class DragonBoneParticle(int style) : Particle
{
	protected readonly int _style = style;
	protected int _timeActive;

	public override void Update(ref ParticleRendererSettings settings)
	{
		const int timeLeft = 300;

		if (++_timeActive >= timeLeft)
			ShouldBeRemovedFromRenderer = true;

		if (_timeActive > timeLeft - 10)
			Scale *= 0.9f;

		Velocity += Vector2.UnitY * 0.08f;
		Scale -= 0.005f;
		Rotation += 0.04f;

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 4, 0, _style, 0, -2);

		spritebatch.Draw(texture, Position + settings.AnchorPosition, source, Color.White, Rotation, source.Size() / 2, Scale, default, 0);
	}
}