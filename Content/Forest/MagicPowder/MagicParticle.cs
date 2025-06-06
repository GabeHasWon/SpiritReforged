using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Forest.MagicPowder;

public class MagicParticle : Particle
{
	private readonly int _frame;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public override ParticleLayer DrawLayer => ParticleLayer.BelowProjectile;

	public MagicParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Color = color;
		Scale = scale;
		MaxTime = maxTime;

		_frame = Main.rand.Next(3);
	}

	public override void Update()
	{
		Lighting.AddLight(Position, Color.ToVector3() * Scale * 0.5f);

		Rotation += Velocity.Length() * 0.01f;
		Velocity = Velocity.RotatedByRandom(0.1f);
		Velocity *= 0.99f;
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = TextureAssets.Projectile[ModContent.ProjectileType<FlarepowderDust>()].Value;
		var position = Position - Main.screenPosition;
		var source = texture.Frame(1, 3, 0, _frame, sizeOffsetY: -2);
		float scaleTimeModifier = EaseFunction.EaseCubicOut.Ease(1 - Progress);

		spriteBatch.Draw(texture, position, source, Color.Additive(), Rotation, source.Size() / 2, Scale * scaleTimeModifier, default, 0);
		spriteBatch.Draw(texture, position, source, Color.White.Additive(), Rotation, source.Size() / 2, Scale * scaleTimeModifier * 0.5f, default, 0);
	}
}