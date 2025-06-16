using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Savanna.Items.HuntingRifle;

public class SmokeCircleParticle : Particle
{
	private readonly int _maxTime;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public SmokeCircleParticle(Vector2 position, Vector2 velocity, Color color, float scale, float rotation, int maxTime)
	{
		Position = position;
		Velocity = velocity;
		Color = color;
		Scale = scale;
		Rotation = rotation;
		_maxTime = maxTime;
	}

	public override void Update()
	{
		Scale += 1f / _maxTime * .3f;

		if (TimeActive > _maxTime)
			Kill();
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var tex = TextureAssets.GlowMask[239].Value;
		float progress = TimeActive / (float)_maxTime;

		Effect effect = AssetLoader.LoadedShaders["DistortDissipateTexture"];
		effect.Parameters["PrimaryColor"].SetValue(((TimeActive < 3) ? Color * 1.5f : Color).ToVector4());
		effect.Parameters["SecondaryColor"].SetValue(((TimeActive < 3) ? Color * 1.5f : Color).ToVector4());
		effect.Parameters["TertiaryColor"].SetValue(((TimeActive < 3) ? Color * 1.5f : Color).ToVector4());
		effect.Parameters["uTexture"].SetValue(tex);
		effect.Parameters["noise"].SetValue(TextureAssets.Extra[193].Value);
		effect.Parameters["Progress"].SetValue(progress);
		effect.Parameters["coordMods"].SetValue(Vector2.One);
		effect.Parameters["distortion"].SetValue(progress);
		effect.Parameters["dissolve"].SetValue(0);
		effect.Parameters["texExponent"].SetValue(.25f);

		var square = new Common.PrimitiveRendering.PrimitiveShape.SquarePrimitive
		{
			Color = Color.White * (1f - progress),
			Height = 1f * tex.Height * Scale,
			Length = .25f * tex.Width * Scale,
			Position = Position - Main.screenPosition,
			Rotation = Rotation,
		};
		Common.PrimitiveRendering.PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}
}
