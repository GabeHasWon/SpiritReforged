using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class BloodHit : Particle
{
	public readonly int variant;

	private readonly Entity _parent;
	private Vector2 _offset;

	public BloodHit(Entity parent, Vector2 offsetFromParent, int maxTime, float rotation, float scale)
	{
		_parent = parent;
		Scale = scale;
		Rotation = rotation;
		MaxTime = maxTime;
		_offset = offsetFromParent;
		variant = Main.rand.Next(3);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		if (_parent is null)
		{
			ShouldBeRemovedFromRenderer = true;
			return;
		}

		if (!_parent.active)
			TimeActive += 2;

		Position = _parent.Center + new Vector2(_offset.X * _parent.direction, _offset.Y) * MathHelper.Lerp(1f, 2.5f, EaseFunction.EaseQuadOut.Ease(Progress));
		Velocity = Vector2.Zero;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Texture;
		Rectangle source = texture.Frame(3, 4, variant, (int)MathHelper.Lerp(1, 4, EaseFunction.EaseQuadOut.Ease(Progress)), -2, -2);
		float rotation = Rotation;

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color.White * (1f - Progress), rotation, source.Size() / 2, Scale, 0f, 0);
	}
}