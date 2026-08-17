using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Content.Basalt.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Basalt;

internal class BasaltBiome : Microbiome
{
	protected override void OnPlace(Point16 point)
	{
		int reps = WorldGen.genRand.Next(5) + 1;

		for (int i = 0; i < reps; ++i)
			point = PlaceMainTunnel(point);
	}

	private static Point16 PlaceMainTunnel(Point16 point)
	{
		const float AngleRange = 1f;

		ushort wallType = WallID.WhiteDynasty;
		int width = WorldGen.genRand.Next(80, 110);
		int height = WorldGen.genRand.Next(60, 95);
		float angle = WorldGen.genRand.NextFloat(-AngleRange, AngleRange);
		Vector2 position = point.ToVector2();

		float[] offsetY = GetRandomizedHeights(width, 0, 8, false);
		float[] heights = GetRandomizedHeights(width, 3, 6, true);

		for (float x = width / -2f; x < width / 2f; ++x)
		{
			Vector2 offset = new Vector2(x * 0.95f, 0).RotatedBy(angle);
			Point pos = (position + offset).ToPoint();
			int indexer = (int)(x + width / 2f);
			pos.Y += (int)offsetY[indexer];
			float factor = Utils.GetLerpValue(width / 2f, 0, Math.Abs(x));
			float heightAdj = MathF.Sqrt(factor);

			for (float y = height / -2f * heightAdj; y < height / 2f * heightAdj; ++y)
			{
				Tile tile = Main.tile[pos.X, pos.Y + (int)y];

				if (tile.WallType == wallType)
					continue;

				tile.HasTile = true;
				tile.TileType = (ushort)ModContent.TileType<BasaltTile>();

				if (Math.Abs(y) <= 1f)
					tile.TileColor = PaintID.WhitePaint;

				if (Math.Abs(y) <= Math.Abs(heights[indexer]) + 8)
				{
					tile.HasTile = false;
					tile.WallType = wallType;
				}
			}
		}

		var newPos = (new Vector2(Main.rand.NextFloat(width / -3f, width / 3f) * 0.95f, 0).RotatedBy(angle) + position).ToPoint16();
		return newPos;
	}

	private static float[] GetRandomizedHeights(int width, int rangeMin, int rangeMax, bool abs)
	{
		float[] offsetY = new float[width];
		int stepLength = WorldGen.genRand.Next(4, 11);
		int currentOffset = WorldGen.genRand.Next(rangeMin, rangeMax + 1) * (abs || WorldGen.genRand.NextBool() ? 1 : -1);

		for (int i = 0; i < width; ++i)
		{
			offsetY[i] = currentOffset;
			stepLength--;

			if (stepLength == 0)
			{
				if (WorldGen.genRand.NextBool(6))
					currentOffset = WorldGen.genRand.Next(-8, 9);
				else
				{
					if (currentOffset < -4)
						currentOffset += WorldGen.genRand.Next(1, 8);
					else if (currentOffset > 4)
						currentOffset -= WorldGen.genRand.Next(1, 8);
					else
						currentOffset += WorldGen.genRand.Next(-4, 4);
				}

				stepLength = WorldGen.genRand.Next(4, 11);
			}
		}

		return offsetY;
	}
}
