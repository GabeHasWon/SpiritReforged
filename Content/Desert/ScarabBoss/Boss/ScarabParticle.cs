using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class ScarabParticle : Particle
{
	private const int FLY_TIME = 180;
	private const int NUM_FRAMES = 2;

	private readonly float _distFromScreen;

	private readonly int _direction;
	private readonly int _animOffset;

	private readonly bool _isBackground;

	private readonly Vector2 _startCamera;

	public ScarabParticle(Vector2 worldPosition, float distFromScreen, int direction, bool isBackground) : base()
	{
		Position = worldPosition;
		_startCamera = worldPosition;
		_distFromScreen = distFromScreen;
		_direction = direction;
		_isBackground = isBackground;
		MaxTime = (int)(FLY_TIME * (!_isBackground ? _distFromScreen : (1 / (1 - _distFromScreen)))) * 4;
		_animOffset = Main.rand.Next(60);
	}

	public override void Update()
	{
		Velocity.X = _direction * (_isBackground ? 24 : 16);
		Velocity.Y -= 0.06f * (_isBackground ? 1 : (1 / (1 - _distFromScreen)));
	}

	public override ParticleLayer DrawLayer => (_isBackground) ? ParticleLayer.BelowWall : ParticleLayer.AbovePlayer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D tex = ParticleHandler.GetTexture(Type);

		float opacity = EaseFunction.EaseCircularOut.Ease(EaseFunction.EaseSine.Ease(Progress));
		opacity *= EaseFunction.EaseCircularOut.Ease(1 - _distFromScreen);
		opacity = EaseFunction.EaseCircularOut.Ease(opacity);

		float scale = (_isBackground) ? _distFromScreen : (1 / (1 - _distFromScreen));
		float zoom = (_isBackground) ? (1 / MathHelper.Lerp(1, Main.GameZoomTarget, _distFromScreen)) : MathHelper.Lerp(Main.GameZoomTarget, Main.GameZoomTarget * 2, _distFromScreen);

		Color drawColor = Color.White;

		drawColor = Color.Lerp(drawColor, Color.Black, Math.Min((_isBackground ? 0.25f : 2) * _distFromScreen, 1));

		int curFrame = (int)((TimeActive + _animOffset) / 3f % NUM_FRAMES);

		Rectangle drawFrame = new(0, 1 + curFrame * (tex.Height / NUM_FRAMES), tex.Width, tex.Height / NUM_FRAMES);

		Vector2 drawPosition = Position - Main.screenPosition;
		drawPosition += (Position - _startCamera) * _distFromScreen * (_isBackground ? -1 : 1);

		spriteBatch.Draw(tex, drawPosition, drawFrame, drawColor * opacity, 0, drawFrame.Size() / 2, scale * zoom, _direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1 - _distFromScreen);
	}
}