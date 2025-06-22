using SpiritReforged.Common.ModCompat;
using static Terraria.ID.BiomeConversionID;

namespace SpiritReforged.Common.TileCommon.Conversion;

public class ConversionHelper
{
	public static void RegisterConversions(int[] types, int conversionType, TileLoader.ConvertTile action)
	{
		foreach (int type in types)
			TileLoader.RegisterConversion(type, conversionType, action);
	}

	/// <summary> Based on <see cref="WorldGen.ConvertTile(int, int, int, bool)"/>.<br/>
	/// Converts all tiles within the specified area, then frames and sends the changes over the network. Necessary for multitiles. </summary>
	public static bool ConvertTiles(int i, int j, int width, int height, int newType, bool frameAndSend = true)
	{
		ushort oldType = Main.tile[i, j].TileType;

		if (oldType == newType)
			return false;

		for (int x = i; x < i + width; x++)
		{
			for (int y = j; y < j + height; y++)
			{
				var t = Main.tile[x, y];
				if (t.TileType != oldType)
					continue;

				t.TileType = (ushort)newType;
			}
		}

		if (frameAndSend)
		{
			WorldGen.RangeFrame(i, j, i + width, j + height);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, width, height);
		}

		return true;
	}

	/// <inheritdoc cref="FindType(int, int, Dictionary{int, int})"/>
	/// <param name="corruption"> The Corruption tile type. </param>
	/// <param name="crimson"> The Crimson tile type. </param>
	/// <param name="hallow"> The Hallowed tile type. </param>
	/// <param name="purity"> The purity tile type. </param>
	/// <param name="purificationPowder"> Whether <paramref name="purity"/> is also used when using Purification Powder. </param>
	public static int FindType(int conversionType, int tileType, int corruption, int crimson, int hallow, int purity, bool purificationPowder = true)
	{
		Dictionary<int, int> sets = [];
		sets.Add(Corruption, corruption);
		sets.Add(Crimson, crimson);
		sets.Add(Hallow, hallow);
		sets.Add(Purity, purity);

		if (purificationPowder)
			sets.Add(PurificationPowder, purity);

		return FindType(conversionType, tileType, sets);
	}

	/// <summary> Returns the appropriate tile type for conversion with consideration for <see cref="ConversionCalls"/>. -1 if none exists. </summary>
	/// <param name="conversionType"> The conversion type, usually passed from <see cref="ModBlockType.Convert"/>. </param>
	/// <param name="tileType"> The tile type being converted. </param>
	public static int FindType(int conversionType, int tileType, Dictionary<int, int> sets)
	{
		int result = -1;

		if (sets.TryGetValue(conversionType, out int dictValue))
			result = dictValue;

		if (ConversionCalls.GetConversionType(conversionType, tileType) is int callValue && callValue != -1)
			result = callValue;

		return result;
	}
}