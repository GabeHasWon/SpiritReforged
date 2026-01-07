using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Desert.Walls;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public partial class ZigguratBiome : Microbiome
{
	private static void Infest(int count, Rectangle bound)
	{
		if (count == 0)
			return;

		WorldMethods.Generate(static (i, j) =>
		{
			FastNoiseLite noise = new(WorldGen.genRand.Next());
			noise.SetFrequency(0.1f);

			if (WorldGen.SolidTile(i, j))
				CreateScarabNest(i, j, WorldGen.genRand.Next(20, 50), WorldGen.genRand.Next(20, 50), noise, 60, -0.25f);

			return true;
		}, count, out _, bound);
	}

	public static void CreateScarabNest(int i, int j, int width, int height, FastNoiseLite noise, float outerThickness, float thickness = 0)
	{
		int halfWidth = width / 2;
		int halfHeight = height / 2;
		Rectangle area = new(i - halfWidth, j - halfHeight, width, height);

		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = j - area.Top; y < area.Bottom; y++)
			{
				float noiseValue = noise.GetNoise(x, y);
				float distance = Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j));
				float distanceLimit = width * height * (0.1f + noiseValue * 0.05f);

				if (distance > distanceLimit)
					continue;

				Tile tile = Main.tile[x, y];
				bool hasTile = WorldGen.SolidOrSlopedTile(tile);

				tile.ClearTile();
				tile.WallType = (ushort)((distance > distanceLimit - outerThickness * 0.5f) ? RedSandstoneBrickCrackedWall.UnsafeType : PaleHiveWall.UnsafeType);

				if (hasTile && (noiseValue < thickness || distance > distanceLimit - outerThickness))
				{
					int type = ModContent.TileType<PaleHive>();

					if (distance > distanceLimit - outerThickness * 0.5f)
						type = ModContent.TileType<CrackedSandstone>();
					else if (noiseValue < -0.8f)
						type = ModContent.TileType<GooBlock>();
					else if (noiseValue < -0.7f)
						type = ModContent.TileType<GooeyHive>();

					tile.ResetToType((ushort)type);
				}
			}
		}

		WorldMethods.Generate(GrowVine, WorldGen.genRand.Next(5, 16), out _, area);
	}

	private static bool GrowVine(int i, int j)
	{
		bool result = false;

		for (int c = 0; c < WorldGen.genRand.Next(1, 5); c++)
			result |= Placer.GrowVine(i, j, ModContent.TileType<GooeyVine>(), 5);

		return result;
	}
}