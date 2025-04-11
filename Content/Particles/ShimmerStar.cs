using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class ShimmerStar : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public ShimmerStar(Vector2 position, Color color, float scale, int maxTime, Vector2 velocity = default)
	{
		Position = position;
		Color = color.Additive();
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
	}

	public override void Update()
	{
		Rotation += .01f;
		Velocity *= .98f;
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var basetexture = ParticleHandler.GetTexture(Type);

		var color = Color * Progress;
		float scale = Scale * (1f - Progress);

		spriteBatch.Draw(basetexture, Position - Main.screenPosition, null, color, Rotation, basetexture.Size() / 2, scale, SpriteEffects.None, 0);
	}
}