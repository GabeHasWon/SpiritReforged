using System.Reflection;

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
}
