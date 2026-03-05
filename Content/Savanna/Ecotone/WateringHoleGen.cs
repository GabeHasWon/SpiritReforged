using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Ecotone;

internal static class WateringHoleGen
{
	/// <summary> Generates a watering hole at the given tile coordinates. </summary>
	/// <param name="i"> The X tile coordinate. </param>
	/// <param name="j"> The Y tile coordinate. </param>
	/// <returns> The area of the watering hole. </returns>
	public static Rectangle GenerateWateringHole(int i, int j)
	{
		const int halfDistance = 35;

		int width = WorldGen.genRand.Next(20, 26);
		var area = new Rectangle(i - width / 2, j, width, WorldGen.genRand.Next(20, 28));

		DigHole(i, j, area.Width, area.Height);

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

		AddWaterAndClay(area);
		return area;
	}

	/// <summary> Fills the watering hole area with water and converts the upper, surrounding tiles to clay. <br/>
	/// Should be used after genpasses that don't interfere with surface water and clay. </summary>
	private static void AddWaterAndClay(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = area.Top; y < area.Bottom; y++)
			{
				var tile = Main.tile[x, y];

				if (!WorldGen.SolidTile(tile) && WaterSafe(x, y))
				{
					int holeDepth = y - area.Top;

					if (holeDepth > 2)
					{
						tile.LiquidAmount = 255;
						tile.LiquidType = LiquidID.Water;
					}

					if (holeDepth < 8 && WorldGen.genRand.NextBool())
						SolidSplotch(x, y + 1, TileID.ClayBlock);

					if (holeDepth < 10 && WorldGen.genRand.NextBool())
					{
						SolidSplotch(x, y + 1, TileID.Sand);
						GrowCatTail(x, y);
					}
				}
			}
		}
	}

	private static void GrowCatTail(int i, int j)
	{
		WorldGen.PlaceCatTail(i, j);

		for (int y = 0; y < 8; y++)
			WorldGen.GrowCatTail(i, j);
	}

	private static void DigHole(int i, int j, int width, int depth)
	{
		i -= width / 2; //Automatically center

		for (int x = 0; x < width; x++)
		{
			int minDepth = (int)(Math.Abs(Math.Sin(x / (float)width * Math.PI)) * depth);
			WorldMethods.FindGround(i + x, ref j);

			if (WaterSafe(i + x, j + minDepth, true))
			{
				for (int y = 0; y < 5; y++) //Line the sides of the hole with sandstone
				{
					var t = Main.tile[i + x, j + y + minDepth];

					t.TileType = TileID.Sandstone;
					t.HasTile = true;
				}
			}

			for (int y = 0; y < minDepth; y++)
			{
				Main.tile[i + x, j + y].ClearEverything();

				Main.tile[i + x - 1, j + y].WallType = WallID.None; //Clear walls to the left and right
				Main.tile[i + x + 1, j + y].WallType = WallID.None;
				Main.tile[i + x - 1, j + y + 1].WallType = WallID.None; //Diagonals
				Main.tile[i + x + 1, j + y + 1].WallType = WallID.None;
			}

			if (minDepth < depth)
			{
				var tile = Framing.GetTileSafely(i + x, j + minDepth);

				if (WorldGen.genRand.NextBool(5) || minDepth > depth - 3)
				{
					tile.IsHalfBlock = true;
					tile.Slope = SlopeType.Solid;
				}
				else
				{
					var slope = (x > width / 2) ? SlopeType.SlopeDownRight : SlopeType.SlopeDownLeft;
					tile.Slope = slope;
				}
			}
		}
	}

	private static bool WaterSafe(int i, int j, bool checkWalls = false)
	{
		if (WorldGen.SolidTile(i - 1, j) || Main.tile[i - 1, j].LiquidAmount == 255 || checkWalls && Main.tile[i - 1, j].WallType != WallID.None)
			return true;

		if (WorldGen.SolidTile(i + 1, j) || Main.tile[i + 1, j].LiquidAmount == 255 || checkWalls && Main.tile[i + 1, j].WallType != WallID.None)
			return true;

		return false;
	}

	private static void SolidSplotch(int i, int j, ushort type)
	{
		i -= 1;

		for (int x = i; x < i + 3; x++)
		{
			for (int y = j; y < j + 4; y++)
			{
				var t = Main.tile[x, y];

				if (WorldGen.SolidOrSlopedTile(t))
					t.TileType = type;
			}
		}
	}
}
