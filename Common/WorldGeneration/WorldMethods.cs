using SpiritReforged.Content.Forest.Misc;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration;

public class WorldMethods
{
	/// <summary> Whether the world is being generated or <see cref="UpdaterSystem"/> is running a generation task. </summary>
	public static bool Generating => WorldGen.generatingWorld || UpdaterSystem.RunningTask;

	internal static int FindNearestBelow(int x, int y)
	{
		while (!WorldGen.SolidTile(x, y))
			y++;

		return y - 1;
	}

	internal static int FindNearestAbove(int x, int y)
	{
		while (!WorldGen.SolidTile(x, y))
		{
			y--;

			if (y < 10)
				return 10;
		}

		return y;
	}

	/// <summary> Scans up, then down for the nearest surface tile. </summary>
	public static int FindGround(int i, ref int j)
	{
		while (j > 20 && WorldGen.SolidOrSlopedTile(i, j - 1))
			j--; //Up

		while (j < Main.maxTilesY - 20 && !WorldGen.SolidOrSlopedTile(i, j))
			j++; //Down

		return j;
	}

	/// <summary> Scans up, then down for the nearest surface tile. </summary>
	public static int FindGround(int i, int j)
	{
		while (j > 20 && WorldGen.SolidOrSlopedTile(i, j - 1))
			j--; //Up

		while (j < Main.maxTilesY - 20 && !WorldGen.SolidOrSlopedTile(i, j))
			j++; //Down

		return j;
	}

	/// <summary> Scans up, then down for the nearest surface tile. Breaks if near any world edge and returns false. </summary>
	public static bool SafeFindGround(int i, ref int j)
	{
		while (WorldGen.SolidOrSlopedTile(i, j - 1))
		{
			if (!WorldGen.InWorld(i, j, 5))
			{
				return false;
			}

			j--; //Up
		}

		while (!WorldGen.SolidOrSlopedTile(i, j))
		{
			if (!WorldGen.InWorld(i, j, 5))
			{
				return false;
			}

			j++; //Down
		}

		return true;
	}

	public static void CragSpike(int X, int Y, int length, int height, ushort type2, float slope, float sloperight)
	{
		float trueslope = 1 / slope;
		float truesloperight = 1 / sloperight;

		for (int level = 0; level <= height; level++)
		{
			Tile tile = Main.tile[X, (int)(Y + level - slope / 2)];
			tile.HasTile = true;
			Main.tile[X, (int)(Y + level - slope / 2)].TileType = type2;
			for (int I = X - (int)(length + level * trueslope); I < X + (int)(length + level * truesloperight); I++)
			{
				Tile tile2 = Main.tile[I, Y + level];
				tile2.HasTile = true;
				Main.tile[I, (Y + level)].TileType = type2;
			}
		}
	}

	public static void RoundHole(int X, int Y, int Xmult, int Ymult, int strength, bool initialdig)
	{
		if (initialdig)
			WorldGen.digTunnel(X, Y, 0, 0, strength, strength, false);

		for (int rotation2 = 0; rotation2 < 350; rotation2++)
		{
			int DistX = (int)(0 - Math.Sin(rotation2) * Xmult);
			int DistY = (int)(0 - Math.Cos(rotation2) * Ymult);

			WorldGen.digTunnel(X + DistX, Y + DistY, 0, 0, strength, strength, false);
		}
	}

	public static int AreaCount(int i, int j, int width, int height, bool countNonSolid)
	{
		int count = 0; 

		for (int x = i; x < i + width; ++x)
		{
			for (int y = j; y < j + height; ++y)
			{
				Tile tile = Framing.GetTileSafely(x, y);

				if (tile.HasTile && (countNonSolid || Main.tileSolid[tile.TileType]))
					count++;
			}
		}

		return count;
	}

	public static bool AreaClear(int i, int j, int width, int height, bool countNonSolid = false) => AreaCount(i, j, width, height, countNonSolid) == 0;

	/// <summary> Checks whether this tile area is completely submerged in water. </summary>
	public static bool Submerged(int i, int j, int width, int height)
	{
		for (int x = i; x < i + width; x++)
		{
			for (int y = j; y < j + height; y++)
			{
				var tile = Framing.GetTileSafely(x, y);
				if (tile.LiquidType != LiquidID.Water || tile.LiquidAmount < 255)
					return false;
			}
		}

		return true;
	}

	public static bool AdjacentOpening(int x, int y)
	{
		for (int i = x - 1; i < x + 2; ++i)
			for (int j = y - 1; j < y + 2; ++j)
				if (!WorldGen.SolidTile(i, j) && (i != x || j != y))
					return true;
		return false;
	}

	public static bool CloudsBelow(int x, int y, out int addY)
	{
		const int scanDistance = 30;
		HashSet<int> types = [TileID.Cloud, TileID.RainCloud, TileID.SnowCloud];

		for (int i = 0; i < scanDistance; i++)
			if (Main.tile[x, y + i].HasTile && types.Contains(Main.tile[x, y + i].TileType))
			{
				addY = scanDistance;
				return true;
			}

		addY = 0;
		return false;
	}

	public static bool IsFlat(Point16 position, int width, out int startY, out int endY, int maxDeviance = 2)
	{
		int maxSamples = (width * 2 + 1) / 4;
		List<int> samples = [];

		for (int i = 0; i < maxSamples; i++)
		{
			int x = (int)MathHelper.Lerp(position.X - width, position.X + width, (float)i / maxSamples);
			int y = position.Y;

			FindGround(x, ref y);
			samples.Add(y);
		}

		startY = samples[0];
		endY = samples[^1];

		int average = (int)samples.Average();
		int surfaceAverage = (int)Math.Abs(MathHelper.Lerp(startY, endY, .5f));

		return Math.Abs(startY - endY) <= maxDeviance && Math.Abs(average - surfaceAverage) <= maxDeviance;
	}

	/// <returns> Whether gen was successful. </returns>
	public delegate bool GenDelegate(int x, int y);
	/// <summary> Selects a random location within <paramref name="area"/> and calls <paramref name="del"/>. </summary>
	/// <param name="del"></param>
	/// <param name="count"> The desired number of items to generate. </param>
	/// <param name="generated"> The actual number of items generated. </param>
	/// <param name="area"> The area to select a point within. Provides a valid default area. </param>
	/// <param name="maxTries"> The unconditional maximum number of locations that can be selected. </param>
	public static void Generate(GenDelegate del, int count, out int generated, Rectangle area = default, int maxTries = 1000)
	{
		int currentCount = 0;

		if (area == default) //Default area
		{
			int top = (int)GenVars.worldSurfaceHigh;
			int left = 20;

			area = new(left, top, Main.maxTilesX - left - 20, Main.maxTilesY - top - 20);
		}

		for (int t = 0; t < maxTries; t++)
		{
			Vector2 random = WorldGen.genRand.NextVector2FromRectangle(area);

			int x = (int)random.X;
			int y = (int)random.Y;

			if (del(x, y) && ++currentCount >= count)
				break;
		}

		generated = currentCount;
	}
}