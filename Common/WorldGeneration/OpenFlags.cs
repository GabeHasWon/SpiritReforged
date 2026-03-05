namespace SpiritReforged.Common.WorldGeneration;

[Flags]
public enum OpenFlags
{
	None = 0,
	Above = 1,
	Below = 2,
	Left = 4,
	Right = 8,
	UpLeft = 16,
	UpRight = 32,
	DownLeft = 64,
	DownRight = 128
}

public static class OpenTools
{
	public static OpenFlags GetOpenings(int i, int j, bool onlyVertical = true, bool noDiagonals = true, bool onlySolid = false)
	{
		OpenFlags flags = OpenFlags.None;

		if (Clear(i, j - 1))
			flags |= OpenFlags.Above;

		if (Clear(i, j + 1))
			flags |= OpenFlags.Below;

		if (onlyVertical)
			return flags;

		if (Clear(i - 1, j))
			flags |= OpenFlags.Left;

		if (Clear(i + 1, j))
			flags |= OpenFlags.Right;

		if (noDiagonals)
			return flags;

		if (Clear(i + 1, j - 1))
			flags |= OpenFlags.UpRight;

		if (Clear(i - 1, j - 1))
			flags |= OpenFlags.UpLeft;

		if (Clear(i - 1, j + 1))
			flags |= OpenFlags.DownLeft;

		if (Clear(i + 1, j + 1))
			flags |= OpenFlags.DownRight;

		return flags;

		bool Clear(int x, int y)
		{
			var tile = Main.tile[x, y];

			if (onlySolid)
				return !WorldGen.SolidOrSlopedTile(tile);

			return !tile.HasTile;
		}
	}
}