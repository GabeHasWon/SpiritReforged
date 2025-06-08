namespace SpiritReforged.Common.ModCompat;

internal class ConversionCalls : ILoadable
{
	public readonly record struct ConversionHolder(int Type, Dictionary<int, int> TileToTileLookup);

	internal static Dictionary<int, ConversionHolder> HandlersByConversionType = [];

	public static bool RegisterConversionTable(object[] args)
	{
		if (args.Length != 2)
			throw new ArgumentException("args should be 2 elements long (int conversionType, Dictionary<int, int> tileToTileConversion)!");

		int conversionType = SpiritReforgedMod.ConvertToInteger(args[0], "RegisterConversionTable parameter 0 should be an int, short or ushort!");

		if (conversionType < BiomeConversionID.Count)
			throw new ArgumentException("Vanilla conversions aren't supported by this call.");

		if (args[1] is not Dictionary<int, int> TileToTileLookup)
			throw new ArgumentException("RegisterConversionTable parameter 1 should be a Dictionary<int, int>!");

		HandlersByConversionType.Add(conversionType, new ConversionHolder(conversionType, TileToTileLookup));
		return true;
	}

	public static bool RegisterConversionTile(object[] args)
	{
		if (args.Length != 3)
			throw new ArgumentException("args should be 3 elements long (int conversionType, int tileType, int convertedType)!");

		int conversionType = SpiritReforgedMod.ConvertToInteger(args[0], "RegisterConversionTile parameter 0 should be an int, short or ushort!");

		if (conversionType < BiomeConversionID.Count)
			throw new ArgumentException("Vanilla conversions aren't supported by this call.");

		int tileType = SpiritReforgedMod.ConvertToInteger(args[1], "RegisterConversionTile parameter 1 should be an int, short or ushort!");
		int convertedType = SpiritReforgedMod.ConvertToInteger(args[2], "RegisterConversionTile parameter 2 should be an int, short or ushort!");

		if (HandlersByConversionType.TryGetValue(conversionType, out ConversionHolder holder))
			holder = new ConversionHolder(conversionType, []);

		holder.TileToTileLookup.Add(tileType, convertedType);
		return true;
	}

	/// <summary>
	/// Gets the registered conversion type based on the conversion ID and the tile ID.
	/// </summary>
	/// <param name="conversionType">Conversion ID to use. Vanilla IDs are not to be added crossmod.</param>
	/// <param name="tileType">Tile ID to convert.</param>
	/// <returns>Converted tile ID. -1 if none are found.</returns>
	public static int GetConversionType(int conversionType, int tileType)
	{
		if (HandlersByConversionType.TryGetValue(conversionType, out ConversionHolder holder) && holder.TileToTileLookup.TryGetValue(tileType, out int newType))
			return newType;

		return -1;
	}

	public void Load(Mod mod) {}
	public void Unload() => HandlersByConversionType.Clear();
}
