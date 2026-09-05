using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Visuals;
namespace SpiritReforged.Content.Particles;

public class LightningBoltParticle : Particle, IDrawPixelated
{
	private VertexTrail[] _trails;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	Color _startColor;
	Color _endColor;

	public LightningBoltParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float rotation, float scale, int maxTime)
	{
		Position = position;
		_startColor = startColor;
		_endColor = endColor;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
	}

	public override void Update()
	{
		if (!Main.dedServ)
		{
			if (_trails == null)
				CreateTrail();

			foreach (VertexTrail trail in _trails)
				trail.Update();
		}

		if (Main.rand.NextBool())
			Velocity = Velocity.RotatedByRandom(3.14f);

		Position += Main.rand.NextVector2CircularEdge(0.4f, 0.4f);

		Velocity *= 0.965f;

		float progress = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);

		Color color = _startColor;
		color *= 0.33f;

		Lighting.AddLight(Position, color.R / 255f * progress, color.G / 255f * progress, color.B / 255f * progress);
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new ParticleTrailPosition(this);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(_startColor, _endColor, EaseFunction.EaseCircularOut), tCap, tPos, tShader, 15 * Scale, 20, 2),
			new VertexTrail(new GradientTrail(Color.White.Additive(), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 12 * Scale, 20, 2),
		];
	}

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		if (_trails != null)
		{
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);
				trail?.Draw(TrailSystem.TrailShaders, spriteBatch.GraphicsDevice, Matrix.Identity);
			}
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var tex = ParticleHandler.GetTexture(Type);

		float progress = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);

		spriteBatch.Draw(tex, Position - Main.screenPosition, null, _startColor with { A = 0 } * 0.05f * progress, 0, tex.Size() / 2, Scale * 0.3f, SpriteEffects.None, 0);
		spriteBatch.Draw(tex, Position - Main.screenPosition, null, _endColor with { A = 0 } * 0.03f * progress, 0, tex.Size() / 2, Scale * 0.25f, SpriteEffects.None, 0);
	}
}
