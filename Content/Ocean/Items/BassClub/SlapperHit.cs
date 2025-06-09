using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Ocean.Items.BassClub;

public class SlapperHit : Particle
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public static readonly Asset<Texture2D> CritTexture = DrawHelpers.RequestLocal(typeof(SlapperHit), "SlapperHit2", false);
	private readonly bool _crit;

	public SlapperHit(Vector2 position, float scale, bool crit = false)
	{
		Position = position;
		Scale = scale;
		_crit = crit;

		Rotation = Main.rand.NextFloat(-0.4f, 0.4f);
		Color = Color.White;
		MaxTime = 12;
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var tex = _crit ? CritTexture.Value : ParticleHandler.GetTexture(Type);
		var frame = tex.Frame(1, 2, frameY: (int)(Progress * 2), sizeOffsetY: -2);
		var color = Color.White * (1f - Progress) * 2;

		spriteBatch.Draw(tex, Position - Main.screenPosition, frame, color, Rotation, frame.Size() / 2, Scale, SpriteEffects.None, 0);
		spriteBatch.Draw(tex, Position - Main.screenPosition, frame, color * 0.25f, Rotation, frame.Size() / 2, Scale * 1.1f, SpriteEffects.None, 0);
	}
}
