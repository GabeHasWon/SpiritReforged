using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class DragonEmber : Particle
{
	private readonly int _style;
	private readonly float _baseScale;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public DragonEmber(Vector2 position, Vector2 velocity, float scale, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Scale = scale;
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		Color = Color.White;
		MaxTime = maxTime;

		_baseScale = scale;
		_style = Main.rand.Next(5);
	}

	public override void Update()
	{
		const int fadeout = 10;
		Lighting.AddLight(Position, Color.Orange.ToVector3() * Scale * 0.5f);

		if (TimeActive > MaxTime - fadeout)
			Scale = _baseScale * (1f - (TimeActive - (float)(MaxTime - fadeout)) / fadeout);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D texture = ParticleHandler.GetTexture(Type);
		Rectangle source = texture.Frame(1, 5, 0, _style, 0, -2);

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color, Rotation, source.Size() / 2, Scale, default, 0);
	}
}