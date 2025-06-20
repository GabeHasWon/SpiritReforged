using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Common.TileCommon.Conversion;

internal class ConversionHelper
{
	public const int AnyPurityID = -8;

	/// <returns> Whether <paramref name="conversionType"/> is either <see cref="BiomeConversionID.Purity"/> or <see cref="BiomeConversionID.PurificationPowder"/>. </returns>
	public static bool AnyPurity(int conversionType) => conversionType is BiomeConversionID.Purity or BiomeConversionID.PurificationPowder;

	public static void RegisterConversions(int[] types, int conversionType, TileLoader.ConvertTile action)
	{
		foreach (int type in types)
			TileLoader.RegisterConversion(type, conversionType, action);
	}

	public static bool Simple(int i, int j, int conversionType, int corruption, int crimson, int hallow, int purity) 
		=> Simple(i, j, conversionType, (BiomeConversionID.Corruption, corruption), (BiomeConversionID.Crimson, crimson), (BiomeConversionID.Hallow, hallow), (AnyPurityID, purity));

	public static bool Simple(int i, int j, int conversionType, params (int, int)[] conversionToType)
	{
		bool value = false;
		foreach (var pair in conversionToType)
		{
			if (conversionType == pair.Item1 || pair.Item1 == AnyPurityID && AnyPurity(conversionType))
			{
				value = DoSimpleConversion(i, j, pair.Item2, conversionType);
				break;
			}
		}

		return value;
	}

	private static bool DoSimpleConversion(int i, int j, int type, int conversionType)
	{
		int startTileType = Main.tile[i, j].TileType;

		if (ConversionCalls.GetConversionType(conversionType, startTileType) is int value && value != -1)
			type = value;

		if (type != -1)
		{
			if (SpiritSets.ConvertsByAdjacent[startTileType])
			{
				if (ConvertAdjacentSet.Converting && ConvertAdjacentSet.CheckAnchors(i, j, type))
				{
					WorldGen.ConvertTile(i, j, type);
					return true;
				}
			}
			else
			{
				WorldGen.ConvertTile(i, j, type);
			}

			return true;
		}

		return false;
	}

	public static bool DoMultiConversion(int i, int j, ushort type)
	{
		ushort startType = Main.tile[i, j].TileType;

		if (startType == type || TileObjectData.GetTileData(startType, 0) is not TileObjectData data)
			return false;

		TileExtensions.GetTopLeft(ref i, ref j);

		for (int x = i; x < i + data.Width; x++)
		{
			for (int y = j; y < j + data.Height; y++)
			{
				var target = Framing.GetTileSafely(x, y);

				if (target.TileType == startType)
					target.TileType = type;
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, data.Width, data.Height);

		WorldGen.RangeFrame(i, j, i + data.Width, j + data.Width);
		return true;
	}
}