using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Forest.Glyphs;
using static SpiritReforged.Content.Forest.Glyphs.RadiantGlyph;

namespace SpiritReforged.Content.Particles;

public class LightFlash : Particle
{
	private Color startColor;
	private Color endColor;
	private Vector2 scale;
	private Vector2 offset;

	private int rotDirection;

	private Entity Parent;

	public bool fromRadiant;

	private readonly Action<Particle> _action;
	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowProjectile;
	public override ParticleLayer DrawLayer => Layer;

	public override ParticleDrawType DrawType => ParticleDrawType.CustomBatchedAdditiveBlend;

	public LightFlash(Entity parent, Vector2 offsetFromParent, Color StartColor, Color EndColor, Vector2 Scale, int maxTime, float rotation, int rotationDirection, Action<Particle> extraUpdateAction = null)
	{
		Parent = parent;
		offset = offsetFromParent;
		startColor = StartColor;
		endColor = EndColor;
		Rotation = rotation;
		scale = Scale;
		MaxTime = maxTime;
		_action = extraUpdateAction;

		rotDirection = rotationDirection;
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

		float gfx = 0f;
		if (Parent is Player)
		{
			var p = Parent as Player;

			gfx = p.gfxOffY;
			// Gross check for radiant glyph specifically so the particles fade quick after striking with Radiant Glyph
			if (fromRadiant && !p.GetModPlayer<RadiantPlayer>().divineStrike)
			{
				Color *= 0.9f;
				TimeActive += 2;
			}				
		}

		Position = Parent.Center + new Vector2(offset.X * Parent.direction, gfx + offset.Y);
		Color = Color.Lerp(startColor, endColor, EaseBuilder.EaseCircularOut.Ease(Progress));
		Velocity = Vector2.Zero;

		Rotation += 0.005f * rotDirection;

		_action?.Invoke(this);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		//Main.NewText(Rotation);

		var tex = ModContent.Request<Texture2D>("SpiritReforged/Content/Forest/Glyphs/RadiantGlyph_Shine").Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float progress = 1f - Progress;

		float xInterp = MathHelper.Lerp(0.9f, 1.1f, EaseBuilder.EaseCircularOut.Ease(1f - progress));
		float yInterp = MathHelper.Lerp(0.9f, 1.2f, EaseBuilder.EaseCircularOut.Ease(1f - progress));

		Vector2 realScale = new Vector2(scale.X * xInterp, scale.Y * yInterp);
		
		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color * progress * 0.4f, Rotation, bloom.Size() / 2f,  realScale.X * 0.35f, SpriteEffects.None, 0);

		spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * progress, Rotation, new Vector2(tex.Width / 2, tex.Height), realScale, SpriteEffects.None, 0);

		spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * progress * 1.5f, Rotation, new Vector2(tex.Width / 2, tex.Height), realScale * 0.66f, SpriteEffects.None, 0);
	}
}
