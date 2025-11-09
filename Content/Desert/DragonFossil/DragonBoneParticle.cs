using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class DragonBoneParticle(int style) : ABasicParticle
{
	public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal(typeof(DragonBoneParticle), "DragonBoneParticle", false);
	private readonly int _style = style;
	private int _timeActive;

	public override void Update(ref ParticleRendererSettings settings)
	{
		const int timeLeft = 300;

		if (++_timeActive >= timeLeft)
			ShouldBeRemovedFromRenderer = true;

		if (_timeActive > timeLeft - 10)
			Scale *= 0.9f;

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture.Value;
		Rectangle source = texture.Frame(1, 4, 0, _style, 0, -2);

		spritebatch.Draw(texture, LocalPosition + settings.AnchorPosition, source, Color.White, Rotation, source.Size() / 2, Scale, default, 0);
	}
}