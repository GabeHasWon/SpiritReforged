using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class DragonBoneParticle : Particle
{
	protected readonly int _style;

	public DragonBoneParticle(int style)
	{
		MaxTime = 300;
		_style = style;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		if (TimeActive > MaxTime - 10)
			Scale *= 0.9f;

		Velocity += Vector2.UnitY * 0.08f;
		Scale -= 0.005f;
		Rotation += 0.04f;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 4, 0, _style, 0, -2);

		spritebatch.Draw(texture, Position + settings.AnchorPosition, source, Color.White, Rotation, source.Size() / 2, Scale, default, 0);
	}
}