using SpiritReforged.Content.Savanna.Tiles.AcaciaTree;
using static SpiritReforged.Common.TileCommon.Conversion.ConversionHandler;

namespace SpiritReforged.Common.ModCompat;

internal static class ConversionCalls
{
	public static bool RegisterConversionSet(object[] args)
	{
		if (args.Length is not 2 and not 3)
			throw new ArgumentException("args must be at least 2 elements long (string name, Dictionary<int, int> pairs) or (string name, int keyType, int resultType)!");

		if (args.Length == 2)
		{
			if (args[0] is not string name)
				throw new ArgumentException("RegisterConversionSet parameter 1 must be a string!");

			if (args[1] is not Dictionary<int, int> dict)
				throw new ArgumentException("RegisterConversionSet parameter 2 must be an int, ushort, short or Dictionary<int, int>");

			AddSet(name, (Set)dict);
		}
		else if (args.Length == 3)
		{
			if (args[0] is not string name)
				throw new ArgumentException("RegisterConversionSet parameter 1 must be a string!");

			if (args[1] is not int or ushort or short)
				throw new ArgumentException("RegisterConversionSet parameter 2 must be an int, ushort, short or Dictionary<int, int>");

			if (args[2] is not int or ushort or short)
				throw new ArgumentException("RegisterConversionSet parameter 3 must be an int, ushort or short");

			AddSet(name, new() { { (int)args[1], (int)args[2] } });
		}
		else
		{
			throw new ArgumentException("args must be at least 2 elements long (string name, Dictionary<int, int> pairs) or (string name, int keyType, int resultType)!");
		}

		return true;
	}

	public static (bool, int) AddSavannaTree(object[] args)
	{
		if (args.Length != 4)
			throw new ArgumentException("args should be 4 elements long (string texturePath, string name, int anchorType, Mod mod)!");

		if (args[0] is not string texturePath)
			throw new ArgumentException("RegisterConversionTable parameter 0 should be a string!");

		if (args[1] is not string name)
			throw new ArgumentException("RegisterConversionTable parameter 1 should be a string!");

		if (args[2] is not Func<int[]> getTileAnchor)
			throw new ArgumentException("RegisterConversionTable parameter 2 should be a Func<int[]>!");

		if (args[3] is not Mod mod)
			throw new ArgumentException("RegisterConversionTable parameter 3 should be a mod!");

		bool success = mod.AddContent(new AcaciaTreeCrossmod(texturePath, name, getTileAnchor));
		int type = -1;

		if (success)
			type = mod.Find<ModTile>(name).Type;

		return (success, type);
	}
}
