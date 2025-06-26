using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;

namespace SpiritReforged.Content.Ocean.Hydrothermal;

public class DissipatingSmoke : Particle
{
	public bool squash = false;

	private Color endColor;
	private float opacity;
	private readonly Vector2 _noiseStretch = new (1);
	private readonly Vector2 _texExponent = new(2, 1);

	public DissipatingSmoke(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Scale = scale;
		Color = endColor = color;
		MaxTime = maxTime;
	}

	public DissipatingSmoke(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float scale, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Scale = scale;
		Color = startColor;
		this.endColor = endColor;
		MaxTime = maxTime;
	}

	public override void Update()
	{
		opacity = EaseFunction.EaseQuadOut.Ease(Progress);
		opacity = (float)Math.Sin(opacity * MathHelper.Pi);

		Scale += .001f;
		Velocity.X += Math.Clamp(Main.windSpeedCurrent * 10f, -1, 1) * .0025f;
		Velocity.Y *= .988f;

		if (squash)
			Rotation = Velocity.ToRotation();
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AboveProjectile;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var value = AssetLoader.LoadedTextures["Smoke"].Value;
		var effect = AssetLoader.LoadedShaders["DistortDissipateTexture"];
		int stretch = 25;

		for (int i = 0; i < 2; i++)
		{
			var color = (i > 0) ? Color.Gray : Color.Lerp(Color, endColor, Progress);

			effect.Parameters["primaryColor"].SetValue(color.ToVector4());
			effect.Parameters["secondaryColor"].SetValue(color.ToVector4());
			effect.Parameters["tertiaryColor"].SetValue(color.ToVector4());
			effect.Parameters["uTexture"].SetValue(value);
			effect.Parameters["noise"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
			effect.Parameters["Progress"].SetValue(Progress);
			effect.Parameters["coordMods"].SetValue(_noiseStretch);
			effect.Parameters["distortion"].SetValue(.4f * EaseFunction.EaseQuadIn.Ease(Progress));

			float texExponent = MathHelper.Lerp(_texExponent.X, _texExponent.Y, opacity);
			effect.Parameters["texExponent"].SetValue(texExponent);

			effect.Parameters["pixellate"].SetValue(false);
			effect.Parameters["dissolve"].SetValue(0);
			effect.Parameters["doDissolve"].SetValue(false);
			effect.Parameters["scroll"].SetValue(Vector2.Zero);
			effect.Parameters["colorLerpExp"].SetValue(1);

			var lightColor = Lighting.GetColor(Position.ToTileCoordinates().X, Position.ToTileCoordinates().Y);
			float scaleMod = (1 + Progress / 2) * ((i > 0) ? .5f : 1f);
			float squashY = squash ? Velocity.Length() * stretch : 0;

			var square = new SquarePrimitive
			{
				Color = lightColor * opacity,
				Height = Scale * value.Height * scaleMod,
				Length = Scale * value.Width * scaleMod + squashY,
				Position = Position - Main.screenPosition,
				Rotation = Rotation,
			};
			PrimitiveRenderer.DrawPrimitiveShape(square, effect);
		}
	}
}
