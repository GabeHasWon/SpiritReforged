using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class DragonEmber : Particle
{
	private readonly int _style;
	private readonly float _baseScale;

	public DragonEmber(Vector2 position, Vector2 velocity, float scale, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Scale = scale;
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		MaxTime = maxTime;

		_baseScale = scale;
		_style = Main.rand.Next(5);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		const int fadeout = 10;
		Lighting.AddLight(Position, Color.Orange.ToVector3() * Scale* 0.5f);

		if (TimeActive > MaxTime - fadeout)
			Scale = _baseScale * (1f - (TimeActive - (float)(MaxTime - fadeout)) / fadeout);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 5, 0, _style, 0, -2);

		spriteBatch.Draw(texture,  Position + settings.AnchorPosition, source, Color.White, Rotation, source.Size() / 2, Scale, default, 0);
	}
}