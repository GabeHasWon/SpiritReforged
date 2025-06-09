namespace SpiritReforged.Common.Visuals;

public static class DrawHelpers
{
	public delegate void DelegateAction(Vector2 positionOffset, Color colorMod);

	public static void DrawChromaticAberration(Vector2 direction, float strength, DelegateAction action)
	{
		for (int i = -1; i <= 1; i++)
		{
			var aberrationColor = i switch
			{
				-1 => new Color(255, 0, 0, 0),
				0 => new Color(0, 255, 0, 0),
				1 => new Color(0, 0, 255, 0),
				_ => Color.White,
			};

			Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * i;
			offset *= strength;

			action.Invoke(offset, aberrationColor);
		}
	}

	public static void DrawGodrays(SpriteBatch spriteBatch, Vector2 position, Color rayColor, float baseLength, float width, int numRays)
	{
		for (int i = 0; i < numRays; i++)
		{
			var ray = AssetLoader.LoadedTextures["Ray"].Value;
			float rotation = i * (MathHelper.TwoPi / numRays) + Main.GlobalTimeWrappedHourly * ((i % 3 + 1f) / 3) - MathHelper.PiOver2; //Half of rays rotate faster, so it looks less like a rotating static image

			float length = baseLength * (float)(Math.Sin((Main.GlobalTimeWrappedHourly + i) * 2) / 5 + 1); //Arbitrary sine function to fluctuate length between rays over time
			var rayscale = new Vector2(width / ray.Width, length / ray.Height);

			spriteBatch.Draw(ray, position, null, rayColor, rotation, new Vector2(ray.Width / 2, 0), rayscale, SpriteEffects.None, 0);
		}
	}

	public static void DrawGodrayStraight(SpriteBatch spriteBatch, Vector2 position, Color rayColor, float baseLength, float width, float rotation)
	{
		var ray = AssetLoader.LoadedTextures["Ray"].Value;
		float length = baseLength * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 2) / 5 + 1); //Arbitrary sine function to fluctuate length between rays over time
		var rayscale = new Vector2(width / ray.Width, length / ray.Height);

		spriteBatch.Draw(ray, position, null, rayColor, rotation, new Vector2(ray.Width / 2, 0), rayscale, SpriteEffects.None, 0);
	}

	/// <summary> Requests the texture of <paramref name="name"/> is the namespace of <paramref name="type"/>. </summary>
	public static Asset<Texture2D> RequestLocal(Type type, string name, bool immediate = false) => ModContent.Request<Texture2D>(RequestLocal(type, name), immediate ? AssetRequestMode.ImmediateLoad : AssetRequestMode.AsyncLoad);
	public static string RequestLocal(Type type, string name) => (type.Namespace + '.' + name).Replace('.', '/');
}