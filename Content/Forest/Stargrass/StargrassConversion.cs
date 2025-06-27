using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Stargrass.Tiles;

namespace SpiritReforged.Content.Forest.Stargrass;

/// <summary> Includes <see cref="Convert"/> helper for common Stargrass tile conversion. Used during worldgen. </summary>
internal static class StargrassConversion
{
	private static bool IsVine(Tile tile) => tile.TileType is TileID.Vines or TileID.VineFlowers;

	public static void Convert(int i, int j)
	{
		if (!WorldGen.InWorld(i, j))
			return;

		var tile = Main.tile[i, j];

		if (tile.TileType == TileID.Grass)
			tile.TileType = (ushort)ModContent.TileType<StargrassTile>();
		else if (tile.TileType == TileID.Plants)
		{
			tile.TileType = (ushort)ModContent.TileType<StargrassFlowers>();
			tile.TileFrameX = (short)(Main.rand.Next(StargrassFlowers.StyleRange) * 18);
		}
		else if (IsVine(tile))
		{
			while (IsVine(Main.tile[i, j - 1]))
				j--;

			while (IsVine(Main.tile[i, j]))
			{
				Main.tile[i, j].TileType = (ushort)ModContent.TileType<StargrassVine>();
				j++;
			}
		}
		else if (tile.TileType == TileID.Sunflower)
		{
			TileExtensions.GetTopLeft(ref i, ref j);

			for (int x = i; x < i + 2; x++)
			{
				for (int y = j; y < j + 4; y++)
				{
					var target = Main.tile[x, y];

					if (target.TileType == TileID.Sunflower)
						target.TileType = (ushort)ModContent.TileType<Starflower>();
				}
			}
		}
	}
}
