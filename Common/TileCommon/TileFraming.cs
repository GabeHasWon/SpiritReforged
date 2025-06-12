using SpiritReforged.Common.WorldGeneration;
using Terraria;
using static SpiritReforged.Common.WorldGeneration.OpenFlags;

namespace SpiritReforged.Common.TileCommon;

internal static class TileFraming
{
	public static OpenFlags GetConnections(int i, int j)
	{
		OpenFlags flags = None;
		var tile = Main.tile[i, j];

		var up = Main.tile[i, j - 1];
		var right = Main.tile[i + 1, j];
		var down = Main.tile[i, j + 1];
		var left = Main.tile[i - 1, j];

		var upRight = Main.tile[i + 1, j - 1];
		var downRight = Main.tile[i + 1, j + 1];
		var downLeft = Main.tile[i - 1, j + 1];
		var upLeft = Main.tile[i - 1, j - 1];

		if (Merge(tile, up))
			flags |= Above;

		if (Merge(tile, right))
			flags |= Right;

		if (Merge(tile, down))
			flags |= Below;

		if (Merge(tile, left))
			flags |= Left;

		if (Merge(tile, upRight) && flags.HasFlag(Above) && flags.HasFlag(Right))
			flags |= UpRight;

		if (Merge(tile, downRight) && flags.HasFlag(Right) && flags.HasFlag(Below))
			flags |= DownRight;

		if (Merge(tile, downLeft) && flags.HasFlag(Below) && flags.HasFlag(Left))
			flags |= DownLeft;

		if (Merge(tile, upLeft) && flags.HasFlag(Left) && flags.HasFlag(Above))
			flags |= UpLeft;

		return flags;

		static bool Merge(Tile thisTile, Tile otherTile) => otherTile.HasTile && (thisTile.TileType == otherTile.TileType || Main.tileMerge[thisTile.TileType][otherTile.TileType]);
	}

	public static bool Gemspark(int i, int j, bool resetFrame)
	{
		if (!WorldGen.InWorld(i, j, 20))
			return false;

		var t = Main.tile[i, j];
		if (t.Slope != SlopeType.Solid && TileID.Sets.HasSlopeFrames[t.TileType])
			return true;

		OpenFlags c = GetConnections(i, j);

		bool up = c.HasFlag(Above);
		bool right = c.HasFlag(Right);
		bool down = c.HasFlag(Below);
		bool left = c.HasFlag(Left);

		bool upRight = c.HasFlag(UpRight);
		bool downRight = c.HasFlag(DownRight);
		bool downLeft = c.HasFlag(DownLeft);
		bool upLeft = c.HasFlag(UpLeft);

		int randomFrame;
		if (resetFrame)
		{
			randomFrame = WorldGen.genRand.Next(3);
			t.Get<TileWallWireStateData>().TileFrameNumber = randomFrame;
		}
		else
		{
			randomFrame = t.TileFrameNumber;
		}

		short frameX = 0;
		short frameY = 0;

		if (!up && down && !left && right && !downRight)
		{
			frameX = 234;
			frameY = 0;
		}
		else if (!up && down && left && !right && !downLeft)
		{
			frameX = 270;
			frameY = 0;
		}
		else if (up && !down && !left && right && !upRight)
		{
			frameX = 234;
			frameY = 36;
		}
		else if (up && !down && left && !right && !upLeft)
		{
			frameX = 270;
			frameY = 36;
		}
		else if (!up && down && left && right && !downLeft && !downRight)
		{
			frameX = 252;
			frameY = 0;
		}
		else if (up && !down && left && right && !upLeft && !upRight)
		{
			frameX = 252;
			frameY = 36;
		}
		else if (up && down && !left && right && !downRight && !upRight)
		{
			frameX = 234;
			frameY = 18;
		}
		else if (up && down && left && !right && !downLeft && !upLeft)
		{
			frameX = 270;
			frameY = 18;
		}
		else if (up && down && left && right && !downLeft && !downRight && !upLeft && !upRight)
		{
			frameX = 252;
			frameY = 18;
		}
		else if (up && down && left && right && !downLeft && downRight && upLeft && upRight)
		{
			frameX = 270;
			frameY = 54;
		}
		else if (up && down && left && right && downLeft && !downRight && upLeft && upRight)
		{
			frameX = 252;
			frameY = 54;
		}
		else if (up && down && left && right && downLeft && downRight && !upLeft && upRight)
		{
			frameX = 270;
			frameY = 72;
		}
		else if (up && down && left && right && downLeft && downRight && upLeft && !upRight)
		{
			frameX = 252;
			frameY = 72;
		}
		else if (up && down && left && right && !downLeft && !downRight && upLeft && upRight)
		{
			frameX = (short)(108 + randomFrame * 18);
			frameY = 36;
		}
		else if (up && down && left && right && downLeft && downRight && !upLeft && !upRight)
		{
			frameX = (short)(108 + randomFrame * 18);
			frameY = 18;
		}
		else if (up && down && left && right && !downLeft && downRight && !upLeft && upRight)
		{
			frameX = 180;
			frameY = (short)(randomFrame * 18);
		}
		else if (up && down && left && right && downLeft && !downRight && upLeft && !upRight)
		{
			frameX = 198;
			frameY = (short)(randomFrame * 18);
		}
		else if (up && down && left && right && !downLeft && downRight && upLeft && !upRight)
		{
			frameX = 288;
			frameY = 72;
		}
		else if (up && down && left && right && downLeft && !downRight && !upLeft && upRight)
		{
			frameX = 306;
			frameY = 72;
		}
		else if (up && down && left && right && !downLeft && !downRight && !upLeft && upRight)
		{
			frameX = 216;
			frameY = 72;
		}
		else if (up && down && left && right && !downLeft && downRight && !upLeft && !upRight)
		{
			frameX = 216;
			frameY = 54;
		}
		else if (up && down && left && right && !downLeft && !downRight && upLeft && !upRight)
		{
			frameX = 234;
			frameY = 72;
		}
		else if (up && down && left && right && downLeft && !downRight && !upLeft && !upRight)
		{
			frameX = 234;
			frameY = 54;
		}
		else if (!up && down && left && right && !downLeft && downRight && !upLeft && !upRight)
		{
			frameX = 306;
			frameY = 36;
		}
		else if (!up && down && left && right && downLeft && !downRight && !upLeft && !upRight)
		{
			frameX = 288;
			frameY = 36;
		}
		else if (up && !down && left && right && !downLeft && !downRight && !upLeft && upRight)
		{
			frameX = 306;
			frameY = 54;
		}
		else if (up && !down && left && right && !downLeft && !downRight && upLeft && !upRight)
		{
			frameX = 288;
			frameY = 54;
		}
		else if (up && down && !left && right && !downLeft && !downRight && !upLeft && upRight)
		{
			frameX = 288;
			frameY = 0;
		}
		else if (up && down && !left && right && !downLeft && downRight && !upLeft && !upRight)
		{
			frameX = 288;
			frameY = 18;
		}
		else if (up && down && left && !right && !downLeft && !downRight && upLeft && !upRight)
		{
			frameX = 306;
			frameY = 0;
		}
		else if (up && down && left && !right && downLeft && !downRight && !upLeft && !upRight)
		{
			frameX = 306;
			frameY = 18;
		}

		if (frameY != 0 && frameX != 0)
		{
			t.TileFrameX = frameX;
			t.TileFrameY = frameY;

			return false;
		}

		return true;
	}
}