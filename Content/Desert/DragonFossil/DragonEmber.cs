using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class DragonEmber : Particle
{
	private readonly int _style;
	private readonly float _baseScale;

	public DragonEmber(Vector2 position, Vector2 velocity, float scale, int maxTime)
	{
		LocalPosition = position;
		Velocity = velocity;
		Scale = new Vector2(scale);
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		TimeMax = maxTime;

		_baseScale = scale;
		_style = Main.rand.Next(5);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		const int fadeout = 10;
		Lighting.AddLight(LocalPosition, Color.Orange.ToVector3() * Scale.Length() * 0.5f);

		if (TimeActive > TimeMax - fadeout)
			Scale = new Vector2(_baseScale * (1f - (TimeActive - (float)(TimeMax - fadeout)) / fadeout));
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(1, 5, 0, _style, 0, -2);

		spriteBatch.Draw(texture,  LocalPosition + settings.AnchorPosition, source, Color.White, Rotation, source.Size() / 2, Scale, default, 0);
	}
}