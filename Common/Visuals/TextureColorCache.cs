using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Misc;
using System.Linq;

namespace SpiritReforged.Common.Visuals;

/// <summary> Caches basic color data of textures for efficiency. </summary>
[Autoload(Side = ModSide.Client)]
internal class TextureColorCache
{
	private static readonly Dictionary<Texture2D, Color[]> ColorCache = [];
	private static readonly Dictionary<Texture2D, Texture2D> SolidTextureCache = [];

	public static Color[] GetColors(Texture2D texture)
	{
		if (ColorCache.TryGetValue(texture, out Color[] value))
			return value;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);

		//Orders colors from darkest to brightest, and excludes fully transparent pixels
		data = [.. data.OrderBy(x => x.ToVector3().Length()).Where(x => x != Color.Transparent)];

		ColorCache.Add(texture, data);
		return data;
	}

	public static Color GetBrightestColor(Texture2D texture) => GetColors(texture)[^1];
	public static Color GetDarkestColor(Texture2D texture) => GetColors(texture)[0];

	public static Texture2D ColorSolid(Texture2D texture, Color color)
	{
		if (SolidTextureCache.TryGetValue(texture, out var textureFromCache))
			return textureFromCache;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);

		for (int i = data.Length - 1; i >= 0; i--)
		{
			if (data[i] != Color.Transparent)
			{
				byte alpha = data[i].A;
				data[i] = color.Additive(alpha);
			}
		}

		var textureToCache = new Texture2D(Main.graphics.GraphicsDevice, texture.Width, texture.Height);
		textureToCache.SetData(data);

		SolidTextureCache.Add(texture, textureToCache);
		return textureToCache;
	}
}