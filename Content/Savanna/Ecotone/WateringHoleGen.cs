﻿using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Ecotone;

internal static class WateringHoleGen
{
	internal static Rectangle Area;

	public static void GenerateWateringHole(int i, int j)
	{
		DigHole(i, j, WorldGen.genRand.Next(20, 26), WorldGen.genRand.Next(20, 28));

		const int halfDistance = 35;
		for (int a = 0; a < 5; a++) //Generate surrounding sand patches
		{
			int x = i + WorldGen.genRand.Next(-halfDistance, halfDistance);
			WorldMethods.FindGround(x, ref j);

			WorldGen.TileRunner(x, j, 10, 1, TileID.Sand);
		}

		for (int x = i - halfDistance; x < i + halfDistance; x++) //Cave in surface sand spots and generate shrubs
		{
			WorldMethods.FindGround(x, ref j);

			var t = Main.tile[x, j];
			if (t.TileType == TileID.Sand)
				t.HasTile = false;

			if (WorldGen.genRand.NextBool(3))
				WorldGen.PlaceTile(x, j, ModContent.TileType<SavannaShrubs>(), true, style: WorldGen.genRand.NextFromList(0, 3, 4));
		}

		return;
	}

	/// <returns> Whether the watering hole exists. </returns>
	public static bool AddWaterAndClay()
	{
		var area = Area;
		if (area.IsEmpty)
			return false;

		for (int x = area.Left; x < area.Right; x++)
			for (int y = area.Top; y < area.Bottom; y++)
			{
				var tile = Main.tile[x, y];

				if (!WorldGen.SolidTile(tile) && WaterSafe(x, y))
				{
					int holeDepth = y - area.Top;

					if (holeDepth > 1)
					{
						tile.LiquidAmount = 255;
						tile.LiquidType = LiquidID.Water;
					}

					if (holeDepth < 8 && WorldGen.genRand.NextBool())
						ClaySplotch(x, y + 1);
				}
			}

		return true;
	}

	private static Rectangle DigHole(int i, int j, int width, int depth)
	{
		i -= width / 2; //Automatically center
		Area = new Rectangle(i, j, width, depth);

		for (int x = 0; x < width; x++)
		{
			int minDepth = (int)(Math.Abs(Math.Sin(x / (float)width * Math.PI)) * depth);
			WorldMethods.FindGround(i + x, ref j);

			for (int y = 0; y < minDepth; y++)
			{
				Main.tile[i + x, j + y].ClearEverything();

				Main.tile[i + x - 1, j + y].WallType = WallID.None; //Clear walls to the left and right
				Main.tile[i + x + 1, j + y].WallType = WallID.None;
				Main.tile[i + x - 1, j + y + 1].WallType = WallID.None;
				Main.tile[i + x + 1, j + y + 1].WallType = WallID.None;
			}

			if (!WaterSafe(i + x, j + minDepth))
				continue;

			for (int y = 0; y < 5; y++) //Line the sides of the hole with sandstone
			{
				var t = Main.tile[i + x, j + y + minDepth];

				t.TileType = TileID.Sandstone;
				t.HasTile = true;
			}
		}

		return Area;
	}

	private static bool WaterSafe(int i, int j)
	{
		if (WorldGen.SolidTile(i - 1, j) || Main.tile[i - 1, j].LiquidAmount == 255)
			return true;

		if (WorldGen.SolidTile(i + 1, j) || Main.tile[i + 1, j].LiquidAmount == 255)
			return true;

		return false;
	}

	private static void ClaySplotch(int i, int j)
	{
		i -= 1;

		for (int x = i; x < i + 3; x++)
			for (int y = j; y < j + 4; y++)
			{
				var t = Main.tile[x, y];

				if (WorldGen.SolidOrSlopedTile(t))
					t.TileType = TileID.ClayBlock;
			}
	}
}
