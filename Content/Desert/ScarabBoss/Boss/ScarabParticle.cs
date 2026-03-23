using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class ScarabParticle : Particle
{
	private const int FLY_TIME = 180;
	private const int NUM_FRAMES = 4;

	private readonly float _distFromScreen;
	private readonly float _acceleration;

	private readonly int _direction;
	private readonly int _animOffset;

	private readonly bool _isBackground;

	private readonly Vector2 _startCamera;

	public ScarabParticle(Vector2 worldPosition, float distFromScreen, int direction, bool isBackground) : base()
	{
		Position = worldPosition;
		_startCamera = Main.screenPosition;
		_distFromScreen = distFromScreen;
		_direction = direction;
		_isBackground = isBackground;
		MaxTime = FLY_TIME;
		_animOffset = Main.rand.Next(60);
		_acceleration = Main.rand.NextFloat(0.04f, 0.12f);
	}

	public override void Update()
	{
		Velocity.X = _direction * (_isBackground ? 10 : 24) * (_acceleration * 6 + 1);
		Velocity.Y -= _acceleration;
	}

	public override ParticleLayer DrawLayer => (_isBackground) ? ParticleLayer.BelowWall : ParticleLayer.AbovePlayer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D tex = ParticleHandler.GetTexture(Type);

		float opacity = EaseFunction.EaseCircularOut.Ease(EaseFunction.EaseSine.Ease(Progress));
		opacity = EaseFunction.EaseCircularOut.Ease(opacity);

		float scale = (_isBackground) ? (1 - _distFromScreen) : (1 + _distFromScreen);

		if (_isBackground)
			scale *= MathHelper.Lerp(1, Main.GameZoomTarget, 1 - _distFromScreen);
		else
			scale *= (float)Math.Pow(Main.GameZoomTarget, 0.5f);

		Color drawColor = Color.White;

		drawColor = Color.Lerp(drawColor, Color.Black, (_isBackground ? 0.5f : 1) * _distFromScreen);

		int curFrame = (int)((TimeActive + _animOffset) / 3f % NUM_FRAMES);

		Rectangle drawFrame = new(0, curFrame * (tex.Height / NUM_FRAMES), tex.Width, (tex.Height / NUM_FRAMES) - 1);
		float parallaxLerper = (float)Math.Pow(_distFromScreen, Main.GameZoomTarget * _distFromScreen);
		if (_isBackground)
			parallaxLerper = MathHelper.Lerp(_distFromScreen, _distFromScreen * _distFromScreen, Main.GameZoomTarget - 1);

		if (_isBackground)
			scale *= MathHelper.Lerp(1, Main.GameZoomTarget, 1 - _distFromScreen);

		Vector2 drawPosition = Position - Vector2.Lerp(Main.screenPosition, _startCamera, parallaxLerper);
		if(!_isBackground)
			drawPosition = Position - Vector2.Lerp(Main.screenPosition, Main.screenPosition - 2 * (_startCamera - Main.screenPosition), 1 - parallaxLerper);

		spriteBatch.Draw(tex, drawPosition, drawFrame, drawColor * opacity, Velocity.X * -0.005f, drawFrame.Size() / 2, scale, _direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1 - _distFromScreen);
	}
}