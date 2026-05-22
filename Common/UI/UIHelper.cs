using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI;

internal static class UIHelper
{
	/// <summary> Frequently used to adjust vanilla inventory elements. Mimics the value of non-public member 'Main.mH'. </summary>
	internal static int GetMapHeight()
	{
		if (!Main.mapEnabled)
			return 0;

		return (int)typeof(Main).GetField("mH", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic).GetValue(null); //Use reflection to get the value
	}

	/// <summary> Wraps <paramref name="text"/> like <see cref="Utils.WordwrapString"/> but with respect for newline. </summary>
	public static string[] WrapText(string text, int bounds)
	{
		if (bounds < 0)
			return [text];

		string[] subText = text.Split('\n');
		List<string> result = [];

		foreach (string line in subText)
		{
			string[] wrapped = Utils.WordwrapString(line, FontAssets.MouseText.Value, bounds, 20, out int length);
			
			for (int i = 0; i < length + 1; i++)
				result.Add(wrapped[i]);
		}

		return [.. result];
	}

	/// <summary> Gets the pixel height of <paramref name="text"/> wrapped by <paramref name="bounds"/>. </summary>
	public static float GetTextHeight(string text, int bounds = 100)
	{
		float height = 0;
		string[] wrappingText = WrapText(text, bounds);

		for (int i = 0; i < wrappingText.Length; i++)
		{
			string line = wrappingText[i];

			if (line is null)
				continue;

			height = FontAssets.MouseText.Value.MeasureString(line).Y / 2;
		}

		return wrappingText.Length * height;
	}

	public static void DrawPanel(SpriteBatch spriteBatch, Texture2D texture, Texture2D borderTexture, Rectangle dimensions, Color? overrideColor = null, Color? overrideBorderColor = null)
	{
		Draw9Slice(spriteBatch, texture, dimensions, overrideColor);
		Draw9Slice(spriteBatch, borderTexture, dimensions, overrideBorderColor ?? Color.Black);
	}

	public static void Draw9Slice(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color? overrideColor = null)
	{
		const int _cornerSize = 12;
		const int _barSize = 4;

		Color color = overrideColor ?? new Color(63, 82, 151) * 0.7f;
		Point point = new Point(dimensions.X, dimensions.Y);
		Point point2 = new Point(point.X + dimensions.Width - _cornerSize, point.Y + dimensions.Height - _cornerSize);
		int width = point2.X - point.X - _cornerSize;
		int height = point2.Y - point.Y - _cornerSize;
		spriteBatch.Draw(texture, new Rectangle(point.X, point.Y, _cornerSize, _cornerSize), new Rectangle(0, 0, _cornerSize, _cornerSize), color);
		spriteBatch.Draw(texture, new Rectangle(point2.X, point.Y, _cornerSize, _cornerSize), new Rectangle(_cornerSize + _barSize, 0, _cornerSize, _cornerSize), color);
		spriteBatch.Draw(texture, new Rectangle(point.X, point2.Y, _cornerSize, _cornerSize), new Rectangle(0, _cornerSize + _barSize, _cornerSize, _cornerSize), color);
		spriteBatch.Draw(texture, new Rectangle(point2.X, point2.Y, _cornerSize, _cornerSize), new Rectangle(_cornerSize + _barSize, _cornerSize + _barSize, _cornerSize, _cornerSize), color);
		spriteBatch.Draw(texture, new Rectangle(point.X + _cornerSize, point.Y, width, _cornerSize), new Rectangle(_cornerSize, 0, _barSize, _cornerSize), color);
		spriteBatch.Draw(texture, new Rectangle(point.X + _cornerSize, point2.Y, width, _cornerSize), new Rectangle(_cornerSize, _cornerSize + _barSize, _barSize, _cornerSize), color);
		spriteBatch.Draw(texture, new Rectangle(point.X, point.Y + _cornerSize, _cornerSize, height), new Rectangle(0, _cornerSize, _cornerSize, _barSize), color);
		spriteBatch.Draw(texture, new Rectangle(point2.X, point.Y + _cornerSize, _cornerSize, height), new Rectangle(_cornerSize + _barSize, _cornerSize, _cornerSize, _barSize), color);
		spriteBatch.Draw(texture, new Rectangle(point.X + _cornerSize, point.Y + _cornerSize, width, height), new Rectangle(_cornerSize, _cornerSize, _barSize, _barSize), color);
	}
}
