using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;

public class UrchinShard : Particle
{
	private readonly float _maxScale;
	private readonly Vector2 _initialVel;

	public UrchinShard(Vector2 position, Vector2 velocity, float scale, int lifetime)
	{
		Position = position;
		Scale = scale;
		_maxScale = scale;
		MaxTime = lifetime;
		Velocity = velocity;
		_initialVel = velocity;
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);
		//Color = Lighting.GetColor(Position.ToTileCoordinates()) * EaseFunction.EaseQuadOut.Ease(1 - Progress);

		Scale = MathHelper.Lerp(_maxScale, 0, EaseFunction.EaseCubicIn.Ease(Progress));
		Velocity = Vector2.Lerp(_initialVel, Vector2.UnitY, EaseFunction.EaseQuadIn.Ease(Progress));
		Rotation += Velocity.X * 0.1f;
	}
}

public class UrchinShardAlt(Vector2 position, Vector2 velocity, float scale, int lifetime) : UrchinShard(position, velocity, scale, lifetime)
{
}