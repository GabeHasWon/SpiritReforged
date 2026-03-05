using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;

public class LightBurst : Particle
{
	/// <summary> Whether this particle should actually emit light. </summary>
	public bool noLight;
	private float _opacity;

	public LightBurst(Vector2 position, float rotation, Color color, float scale, int maxTime)
	{
		Position = position;
		Color = color.Additive();
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		_opacity = 0;
	}

	public override void Update()
	{
		_opacity = (float)Math.Sin(EaseFunction.EaseCubicOut.Ease(Progress) * MathHelper.Pi);

		if (!noLight)
			Lighting.AddLight(Position, Color.ToVector3() * _opacity);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D rayTexture = AssetLoader.LoadedTextures["Ray"].Value;
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;
		var center = Position - Main.screenPosition;
		Color color = Color * _opacity;

		int numRays = 5;
		for (int i = 0; i < numRays; i++)
		{
			var rayScale = new Vector2(1f, 0.5f);
			var origin = new Vector2(rayTexture.Width / 2, 0);
			float rotation = TimeActive / 40f + MathHelper.TwoPi * i / numRays;
			if (i % 3 == 0) //Smaller inverse rotation rays
			{
				rayScale.Y *= 0.9f;
				rotation = -rotation + TimeActive / 80f;
			}

			Main.spriteBatch.Draw(rayTexture, center, null, color * _opacity, rotation + Rotation, origin, rayScale * Scale * _opacity, SpriteEffects.None, 0);
		}

		Main.spriteBatch.Draw(bloomtexture, center, null, color, 0, bloomtexture.Size() / 2, 0.5f * Scale, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(bloomtexture, center, null, color * 0.5f * _opacity, 0, bloomtexture.Size() / 2, 0.5f * Scale * _opacity, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(bloomtexture, center, null, color * 0.5f * _opacity, 0, bloomtexture.Size() / 2, 0.4f * Scale * _opacity, SpriteEffects.None, 0);
	}

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;
}
