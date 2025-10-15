using SpiritReforged.Content.Forest.Misc;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration;

public static class WorldMethods
{
	/// <summary> Whether the world is being generated or <see cref="UpdaterSystem"/> is running a generation task. </summary>
	public static bool Generating => WorldGen.generatingWorld || UpdaterSystem.RunningTask;
	public static readonly HashSet<int> CloudTypes = [TileID.Cloud, TileID.RainCloud, TileID.SnowCloud];

	/// <summary> Scans up, then down for the nearest surface tile. </summary>
	/// <returns> Whether the coordinates are within world bounds. </returns>
	public static bool FindGround(int i, ref int j)
	{
		const int padding = 20;
		while (j > padding && WorldGen.SolidOrSlopedTile(i, j - 1))
		{
			if (--j == padding) //Move up
				return false;
		}

		while (j < Main.maxTilesY - padding && !WorldGen.SolidOrSlopedTile(i, j))
		{
			if (++j == Main.maxTilesY - padding) //Move down
				return false;
		}

		return true;
	}

	/// <summary><inheritdoc cref="FindGround(int, ref int)"/></summary>
	/// <returns> The final coordinate Y. </returns>
	public static int FindGround(int i, int j)
	{
		FindGround(i, ref j);
		return j;
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
				Main.tile[I, Y + level].TileType = type2;
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

	/// <summary> Gets the number of solid and non-solid tiles in the provided area. </summary>
	public static int AreaCount(int left, int top, int width, int height, bool countNonSolid)
	{
		int count = 0; 

		for (int x = left; x < left + width; ++x)
		{
			for (int y = top; y < top + height; ++y)
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

	public static bool CloudsBelow(int x, int y, out int addY)
	{
		const int scanDistance = 30;

		for (int i = 0; i < scanDistance; i++)
		{
			if (Main.tile[x, y + i].HasTile && CloudTypes.Contains(Main.tile[x, y + i].TileType))
			{
				addY = scanDistance;
				return true;
			}
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

	/// <summary> Iterates over every location within <paramref name="area"/> and calls <paramref name="del"/> for each. </summary>
	public static void GenerateSquared(GenDelegate del, out int generated, Rectangle area = default)
	{
		generated = 0;

		if (area == default) //Default area
		{
			int top = (int)GenVars.worldSurfaceHigh;
			int left = 20;

			area = new(left, top, Main.maxTilesX - left - 20, Main.maxTilesY - top - 20);
		}

		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = area.Top; y < area.Bottom; y++)
			{
				if (del(x, y))
					generated++;
			}
		}
	}

	public static void ApplyTileArea(GenDelegate del, Rectangle area)
	{
		for (int x = area.X; x < area.X + area.Width; x++)
			for (int y = area.Y; y < area.Y + area.Height; y++)
				del.Invoke(x, y);
	}

	public static void ApplyTileArea(GenDelegate del, int startX, int endX, int startY, int endY) => ApplyTileArea(del, new Rectangle(startX, startY, endX - startX, endY - startY));

	/// <summary> Calls <paramref name="del"/> for each connected air tile originating from the given coordinates. Using <paramref name="limit"/> is recommended. </summary>
	/// <param name="limit"> An optional limit on how far to scan. </param>
	public static void ApplyOpenArea(GenDelegate del, int i, int j, Rectangle limit = default)
	{
		if (limit != default) //Adjust limit to adhere to world bounds if necessary
		{
			limit.X = Math.Clamp(limit.X, 20, Main.maxTilesX - 20);
			limit.Y = Math.Clamp(limit.Y, 20, Main.maxTilesY - 20);

			if (limit.BottomLeft().X > Main.maxTilesX - 20)
				limit.Width = Main.maxTilesX - 20 - limit.X;

			if (limit.BottomLeft().Y > Main.maxTilesY - 20)
				limit.Height = Main.maxTilesY - 20 - limit.Y;
		}

		HashSet<Point16> points = [];

		ScanX(i, j); //Start the feedback loop

		foreach (var pt in points)
			del.Invoke(pt.X, pt.Y);

		bool ScanX(int x, int y)
		{
			bool any = false;

			for (int s = 0; s < 2; s++)
			{
				int _x = x;
				int _y = y;

				while (Contained(_x, _y) && !Main.tile[_x, _y].HasTile)
				{
					any |= points.Add(new(x, y));

					if (any)
						ScanY(_x, _y);

					_x += (s == 0) ? 1 : -1;
				}
			}

			return any;
		}

		bool ScanY(int x, int y)
		{
			bool any = false;

			for (int s = 0; s < 2; s++)
			{
				int _x = x;
				int _y = y;

				while (Contained(_x, _y) && !Main.tile[_x, _y].HasTile)
				{
					any |= points.Add(new(x, y));

					if (any)
						ScanX(_x, _y);

					_y += (s == 0) ? 1 : -1;
				}
			}

			return any;
		}

		bool Contained(int x, int y)
		{
			return limit == default || limit.Contains(x, y);
		}
	}
}