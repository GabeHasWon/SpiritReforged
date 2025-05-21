using SpiritReforged.Common.Misc;
using System.Linq;

namespace SpiritReforged.Common.Visuals;

/// <summary> Caches basic color data of textures for efficiency. </summary>
[Autoload(Side = ModSide.Client)]
internal class TextureColorCache
{
	private static readonly Dictionary<Texture2D, Color> BrightestColorCache = [];
	private static readonly Dictionary<Texture2D, Color> DarkestColorCache = [];
	private static readonly Dictionary<Texture2D, Texture2D> SolidTextureCache = [];

	public static Color GetBrightestColor(Texture2D texture)
	{
		if (BrightestColorCache.TryGetValue(texture, out Color value))
			return value;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);
		var brightest = data.OrderByDescending(x => x.ToVector3().Length()).FirstOrDefault();

		if (brightest == default)
			brightest = Color.White;

		BrightestColorCache.Add(texture, brightest);
		return brightest;
	}
	public static Color GetDarkestColor(Texture2D texture)
	{
		if (DarkestColorCache.TryGetValue(texture, out Color value))
			return value;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);
		var darkest = data.OrderBy(x => x.ToVector3().Length()).FirstOrDefault(x => x != default);

		if (darkest == default)
			darkest = Color.White;

		DarkestColorCache.Add(texture, darkest);
		return darkest;
	}

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