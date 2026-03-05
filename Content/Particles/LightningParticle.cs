using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Particles;

public class LightningParticle : Particle
{
	private Vector2 _endPosition;
	private Vector2 _distortion;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public LightningParticle(Vector2 positionStart, Vector2 positionEnd, Color color, int maxTime, float scale)
	{
		Position = positionStart;
		_endPosition = positionEnd;
		Color = color;
		MaxTime = maxTime;
		Scale = scale;

		float distortBase = Main.rand.NextFloat(0.5f, 1.25f) * (Main.rand.NextBool() ? -1 : 1);
		_distortion = new Vector2(distortBase, -distortBase);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		float length = Vector2.Distance(Position, _endPosition);
		float rotation = (_endPosition - Position).ToRotation();
		var drawColor = Color.Lerp(Color.White, Color, EaseFunction.EaseQuadIn.Ease(Progress)).Additive();

		Effect beamEffect = AssetLoader.LoadedShaders["Lightning"].Value;
		beamEffect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["Lightning"].Value);
		beamEffect.Parameters["perlinNoise"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		beamEffect.Parameters["Progress"].SetValue(Progress);
		beamEffect.Parameters["uTime"].SetValue(-EaseFunction.EaseQuadOut.Ease(Progress) / 1.5f);

		beamEffect.Parameters["textureStretch"].SetValue(new Vector2(length / 150f, 0.25f));
		beamEffect.Parameters["noiseStretch"].SetValue(new Vector2(length * 0.3f, Scale * 0.15f) / 50);
		beamEffect.Parameters["exponentRange"].SetValue(new Vector2(1.5f, 5f));
		beamEffect.Parameters["distortRange"].SetValue(_distortion);

		beamEffect.CurrentTechnique.Passes[0].Apply();

		var beam = new SquarePrimitive
		{
			Height = Scale * MathHelper.Lerp(1, 0.5f, EaseFunction.EaseCubicIn.Ease(Progress)),
			Length = length,
			Rotation = rotation,
			Position = Vector2.Lerp(Position, _endPosition, 0.5f) - Main.screenPosition,
			Color = drawColor * (1 - EaseFunction.EaseCircularIn.Ease(Progress))
		};

		PrimitiveRenderer.DrawPrimitiveShape(beam, beamEffect);
	}
}
