using SpiritReforged.Content.Ziggurat.Tiles.Chains;

namespace SpiritReforged.Common.Visuals;

public static class Graphics
{
	public static readonly BlendState AdditiveNoAlpha = new()
	{
		AlphaBlendFunction = BlendFunction.Add,
		ColorBlendFunction = BlendFunction.Add,
		ColorSourceBlend = Blend.SourceAlpha,
		ColorDestinationBlend = Blend.One,
		AlphaSourceBlend = Blend.SourceAlpha,
		AlphaDestinationBlend = Blend.One,
		ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
		ColorWriteChannels1 = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
		ColorWriteChannels2 = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
		ColorWriteChannels3 = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue
	};

	public delegate void ChromaticAction(Vector2 positionOffset, Color colorMod);
	public static Color MulticolorLerp(float increment, params Color[] colors)
	{
		increment %= 0.999f;
		int currentColorIndex = (int)(increment * colors.Length);
		Color color = colors[currentColorIndex];
		Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
		return Color.Lerp(color, nextColor, increment * colors.Length % 1f);
	}

	public static void DrawChromaticAberration(Vector2 direction, float strength, ChromaticAction action)
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

	public static void DrawOutline(Action<Vector2> action) => DrawOutline(default, default, default, default, action);

	/// <summary>Converts the provided namespace to file path syntax.</summary>
	public static string Path(this string value) => value.Replace('.', '/');

	/// <summary>Simply calls <see cref="ModContent.Request{T}(string, AssetRequestMode)"/> for a texture on the given path.</summary>
	[Obsolete("Prefer Graphics.Texture or Graphics.Path extensions")]
	public static Asset<Texture2D> Tex(string path, AssetRequestMode mode = AssetRequestMode.AsyncLoad) => ModContent.Request<Texture2D>(path, mode);

	public static Asset<Texture2D> Texture(this Type type, string value, AssetRequestMode mode = AssetRequestMode.AsyncLoad) => ModContent.Request<Texture2D>((type.Namespace + $".{value}").Path(), mode);
}