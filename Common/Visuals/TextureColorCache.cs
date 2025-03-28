﻿using System.Linq;

namespace SpiritReforged.Common.Visuals;

/// <summary> Caches basic color data of textures for efficiency. </summary>
[Autoload(Side = ModSide.Client)]
internal class TextureColorCache
{
	private static readonly Dictionary<Texture2D, Color> brightestColorCache = [];

	public static Color GetBrightestColor(Texture2D texture)
	{
		if (brightestColorCache.TryGetValue(texture, out Color value))
			return value;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);
		var brightest = data.OrderBy(x => x.ToVector3().Length()).FirstOrDefault();

		if (brightest == default)
			brightest = Color.White;

		brightestColorCache.Add(texture, brightest);
		return brightest;
	}
}
