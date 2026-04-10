using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Particles;

public class CartoonHit : Particle
{
	public static readonly Asset<Texture2D> Grayscale = DrawHelpers.RequestLocal<CartoonHit>("CartoonHit_Grayscale", false);

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public readonly int style;
	private readonly bool _recolorable;

	public CartoonHit(Vector2 position, int duration, float scale = 1, float rotation = 0, Vector2? velocity = null, Color color = default)
	{
		Position = position;
		Scale = scale;
		Rotation = rotation;
		MaxTime = duration;

		_recolorable = color != default;

		Color = _recolorable ? color : Color.White;
		Velocity = velocity ?? Vector2.Zero;
		style = Main.rand.Next(3);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D texture = _recolorable ? Grayscale.Value : Texture;
		Rectangle source = texture.Frame(1, 3, 0, style, 0, -2);

		float rotation = Rotation - MathHelper.PiOver2;
		float scale = Scale * (1f - Progress);

		spriteBatch.Draw(texture, Position - Main.screenPosition + new Vector2(2 * scale).RotatedBy(rotation), source, Color * 0.5f * (1f - Progress), rotation, source.Size() / 2, scale, 0, 0);
		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color, rotation, source.Size() / 2, scale, 0, 0);
	}
}
