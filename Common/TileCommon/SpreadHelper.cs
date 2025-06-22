using System.Linq;

namespace SpiritReforged.Common.TileCommon;

public static class SpreadHelper
{
	public static bool Spread(int i, int j, int type, int chance, params int[] validAdjacentTypes)
	{
		if (Main.rand.NextBool(chance))
		{
			var adjacents = OpenAdjacents(i, j, true, validAdjacentTypes);

			if (adjacents.Count == 0)
				return false;

			Point p = adjacents[Main.rand.Next(adjacents.Count)];
			Framing.GetTileSafely(p.X, p.Y).TileType = (ushort)type;

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, p.X, p.Y, 1, TileChangeType.None);
			return true;
		}

		return false;
	}

	public static void ConversionSpread(int i, int j, int conversionType)
	{
		if (Main.rand.NextBool(4))
		{
			int rand = Main.rand.Next(4);

			if (rand == 0)
				SpreadConversion(i + 1, j, conversionType);
			else if (rand == 1)
				SpreadConversion(i - 1, j, conversionType);
			else if (rand == 2)
				SpreadConversion(i, j - 1, conversionType);
			else if (rand == 3)
				SpreadConversion(i, j + 1, conversionType);
		}
	}

	private static void SpreadConversion(int i, int j, int conversionType)
	{
		WorldGen.Convert(i, j, conversionType, 0);

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 3, TileChangeType.None); //Try spread normal grass
	}

	public static List<Point> OpenAdjacents(int i, int j, bool requiresAir, params int[] types)
	{
		var p = new List<Point>();
		for (int k = -1; k < 2; ++k)
			for (int l = -1; l < 2; ++l)
				if (!(l == 0 && k == 0) && Framing.GetTileSafely(i + k, j + l).HasTile && types.Contains(Framing.GetTileSafely(i + k, j + l).TileType))
					if (!requiresAir || OpenToAir(i + k, j + l))
						p.Add(new Point(i + k, j + l));

		return p;
	}

	public static bool OpenToAir(int i, int j)
	{
		for (int k = -1; k < 2; ++k)
			for (int l = -1; l < 2; ++l)
				if (!(l == 0 && k == 0) && !WorldGen.SolidOrSlopedTile(i + k, j + l))
					return true;

		return false;
	}
}
