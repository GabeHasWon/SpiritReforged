using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Glyphs.Sanguine;

public class SanguineBlood : Particle, IDrawPixelated
{
	private readonly Vector2[] _oldPosition;

	private readonly Player _owner;

	public SanguineBlood(Player owner, Vector2 position, Vector2 velocity, float scale, int maxTime)
	{
		_owner = owner;
		Position = position;
		Scale = scale;
		Color = Color.White;
		Velocity = velocity;
		MaxTime = maxTime;

		_oldPosition = new Vector2[25];
		for (int i = 0; i < _oldPosition.Length; i++)
			_oldPosition[i] = position;
	}

	public override void Update()
	{
		float velocityLength = MathHelper.Lerp(12, 0, Progress);
		float magnetFactor = MathHelper.Lerp(0, 1, (float)Math.Pow(Progress, 1.25f));

		Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
		Velocity = Vector2.Normalize(Vector2.Lerp(Velocity, (-Position).SafeNormalize(Vector2.Zero) * velocityLength, 0.01f)) * Velocity.Length();
		Position = Vector2.Lerp(Position, Vector2.Zero, magnetFactor);

		if(TimeActive == MaxTime / 2)
		{
			for(int i = 0; i < 2; i++)
			{
				Vector2 stickyBloodPos = _owner.MountedCenter + Main.rand.NextVector2Square(-20, 20);

				ParticleHandler.SpawnParticle(new StickyBloodParticle(stickyBloodPos, stickyBloodPos.DirectionFrom(_owner.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 2f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(30, 40), 0.1f));
			}

			ParticleHandler.SpawnParticle(new SmokeCloud(_owner.MountedCenter, -Vector2.UnitY, Color.DarkRed * 0.5f, 0.09f, EaseFunction.EaseQuadOut, 60, false)
			{
				Pixellate = true,
				PixelDivisor = 2,
				Layer = ParticleLayer.AbovePlayer
			});
		}

		for (int i = _oldPosition.Length - 1; i > 0; i--)
			_oldPosition[i] = _oldPosition[i - 1];

		_oldPosition[0] = Position;
	}

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
	}

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		Effect effect = SpiritReforgedMod.Instance.Assets.Request<Effect>("Assets/Shaders/BloodTrail", AssetRequestMode.ImmediateLoad).Value;
		Texture2D uTex = AssetLoader.LoadedTextures["swirlNoise2"].Value;
		effect.Parameters["uTexture"].SetValue(uTex);
		effect.Parameters["uTexture2"].SetValue(AssetLoader.LoadedTextures["GlowTrail"].Value);
		effect.Parameters["scroll"].SetValue(-Progress);
		effect.Parameters["uColorLight"].SetValue(new Color(191, 23, 37).ToVector4());
		effect.Parameters["uColorDark"].SetValue(new Color(112, 1, 25).ToVector4());
		effect.Parameters["progress"].SetValue(1 - EaseFunction.EaseQuadIn.Ease(EaseFunction.EaseSine.Ease(Progress)));

		Vector2[] vertices = new Vector2[_oldPosition.Length];
		float stripLength = 0;

		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = _oldPosition[i] + _owner.MountedCenter;

			if (i == 0)
				continue;

			stripLength += Vector2.Distance(vertices[i], vertices[i - 1]);
		}

		effect.Parameters["repeats"].SetValue(5f * stripLength / uTex.Width);

		var strip = new PrimitiveStrip
		{
			Color = Lighting.GetColor(vertices[0].ToTileCoordinates()),
			Width = 12 * Scale,
			PositionArray = vertices
		};
		PrimitiveRenderer.DrawPrimitiveShape(strip, effect, pixelTargetActive: true);
	}
}