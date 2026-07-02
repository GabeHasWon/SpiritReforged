using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using static SpiritReforged.Content.Glyphs.Radiant.RadiantGlyph;

namespace SpiritReforged.Content.Particles;
public class BloodHit : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public override ParticleLayer DrawLayer => ParticleLayer.BelowNPC;

	public readonly int variant;
	private readonly Entity Parent;

	private Vector2 offset;
	public BloodHit(Entity parent, Vector2 offsetFromParent, int maxTime, float rotation, float scale)
	{
		Parent = parent;
		Scale = scale;
		Rotation = rotation;
		MaxTime = maxTime;
		offset = offsetFromParent;

		Color = Color.White;
		variant = 1 + Main.rand.Next(3);

		if (variant == 1)
			Scale *= 0.7f;
	}

	public override void Update()
	{
		if (Parent is null)
		{
			Kill();
			return;
		}

		if (!Parent.active)
		{
			Color *= 0.9f;
			TimeActive += 2;
		}

		Position = Parent.Center + new Vector2(offset.X * Parent.direction, offset.Y) * MathHelper.Lerp(1f, 2.5f, EaseBuilder.EaseQuadOut.Ease(Progress));
		Velocity = Vector2.Zero;
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = ModContent.Request<Texture2D>("SpiritReforged/Content/Particles/BloodHit_0" + variant).Value;
		
		Rectangle source = texture.Frame(1, 4, 0, (int)MathHelper.Lerp(1, 4, EaseBuilder.EaseQuadOut.Ease(Progress)));

		float rotation = Rotation;

		SpriteEffects flip = SpriteEffects.None;
		if (offset.X < 0)
			flip = SpriteEffects.FlipHorizontally;

		spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color.White * (1f - Progress), rotation, source.Size() / 2, Scale, 0f, 0);
	}
}
