using static SpiritReforged.Common.TileCommon.Conversion.ConversionHandler;
using static Terraria.ID.BiomeConversionID;

namespace SpiritReforged.Common.TileCommon.Conversion;

public static class ConversionHelper
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
			WorldGen.RangeFrame(i, j, i + width - 1, j + height - 1);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, width, height);
		}

		return true;
	}

	/// <summary> Creates a basic set using the provided info, keyed by <see cref="BiomeConversionID"/>s. </summary>
	/// <param name="corruption"> The Corruption tile type. </param>
	/// <param name="crimson"> The Crimson tile type. </param>
	/// <param name="hallow"> The Hallowed tile type. </param>
	/// <param name="purity"> The purity tile type. </param>
	/// <param name="purificationPowder"> Whether <paramref name="purity"/> is also used when using Purification Powder. </param>
	public static Set CreateSimple(int corruption, int crimson, int hallow, int purity, bool purificationPowder = true)
	{
		Set set = [];
		set.Add(Corruption, corruption);
		set.Add(Crimson, crimson);
		set.Add(Hallow, hallow);
		set.Add(Purity, purity);

		if (purificationPowder)
			set.Add(PurificationPowder, purity);

		return set;
	}
}