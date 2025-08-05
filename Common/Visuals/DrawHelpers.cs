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

	public static void DrawOutline(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, Action<Vector2> action = null)
	{
		for (int i = 0; i < 4; i++)
		{
			Vector2 offset = i switch
			{
				1 => new(0, -2),
				2 => new(2, 0),
				3 => new(0, 2),
				_ => new(-2, 0)
			};

			if (action is null)
				spriteBatch.Draw(texture, position + offset, null, color, 0, texture.Size() / 2, 1, default, 0);
			else
				action.Invoke(offset);
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
	/// <inheritdoc cref="RequestLocal(Type, string, bool)"/>
	public static string RequestLocal(Type type, string name) => (type.Namespace + '.' + name).Replace('.', '/');
}