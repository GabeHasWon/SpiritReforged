using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items.Zipline;

internal static class MagnetCursor
{
	//Cache to prevent continuous, unecessary tile checks.
	private static Point16 LastCursorPos;
	private static Point16 LastMagnetizePos;

	public static Point16 Magnetize(int range, out bool failed)
	{
		failed = true;
		var start = Main.MouseWorld.ToTileCoordinates16();

		if (start == LastCursorPos)
		{
			failed = !ScanSurrounding(LastMagnetizePos);
			return LastMagnetizePos;
		}

		LastCursorPos = start;
		List<Point16> points = [];

		for (int x = start.X - range; x < start.X + range; x++)
		{
			for (int y = start.Y - range; y < start.Y + range; y++)
				points.Add(new Point16(x, y));
		}

		foreach (var pt in points.OrderBy(x => x.ToVector2().Distance(start.ToVector2())))
		{
			if (ScanSurrounding(pt))
			{
				failed = false;
				return LastMagnetizePos = pt;
			}
		}

		return LastMagnetizePos = start;
	}

	/// <summary> Checks for solid tiles around <paramref name="coords"/>. </summary>
	public static bool ScanSurrounding(Point16 coords)
	{
		var flags = OpenTools.GetOpenings(coords.X, coords.Y, false, onlySolid: true);
		return !flags.HasFlag(OpenFlags.Above | OpenFlags.Right | OpenFlags.Below | OpenFlags.Left);
	}
}
