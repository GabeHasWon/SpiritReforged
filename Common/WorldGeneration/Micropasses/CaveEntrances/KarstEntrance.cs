using ReLogic.Utilities;
using SpiritReforged.Common.WorldGeneration.Noise;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;

internal class KarstEntrance : CaveEntrance
{
	public override CaveEntranceType Type => CaveEntranceType.Karst;

	public override void Generate(int x, int y)
	{
		y += 16;
		ShapeData data = new();

		WorldUtils.Gen(new Point(x, y), new Shapes.Circle(16, 8),
			Actions.Chain(new Actions.ClearWall().Output(data), new Modifiers.Expand(3), new Modifiers.Blotches(3, 2, 0.6f),
			new Actions.ClearTile().Output(data)));

		int count = WorldGen.genRand.Next(1, 4);

		for (int i = 1; i < count + 1; ++i)
		{
			var origin = new Point(x + WorldGen.genRand.Next(-5 * i, 5 * i + 1), y + WorldGen.genRand.Next(2, 5 * i));
			bool circle = WorldGen.genRand.NextBool();
			GenShape shape = circle ? new Shapes.Circle(12 + 3 * i, 3 + i) : new Shapes.Rectangle(12 + 3 * i, 4 + i);
			var blotches = new Modifiers.Blotches(circle ? 3 : 7, circle ? 2 : 4, circle ? 0.9f : 0.6f);
			WorldUtils.Gen(origin, shape, Actions.Chain(blotches, new Actions.ClearTile().Output(data)));
		}

		WorldUtils.Gen(new Point(x, y - 26), new Shapes.Rectangle(8, 25),
			Actions.Chain(new Modifiers.Blotches(4, 7, 0.2f), new Actions.ClearWall(), new Modifiers.Blotches(5, 3, 0.6f), new Actions.ClearTile().Output(data)));

		foreach (var pos in data.GetData())
		{
			if (pos.Y > 0)
			{
				Tile tile = Main.tile[new Point16(pos.X + x, pos.Y + y)];
				tile.LiquidAmount = 255;
				tile.LiquidType = LiquidID.Water;
			}

			for (int i = pos.X - 4 + x; i < pos.X + 4 + x; i++)
				for (int j = pos.Y - 4 + y; j < pos.Y + 4 + y; ++j)
				{
					if (data.Contains(i, j))
						continue;

					Tile tile = Main.tile[i, j];

					if (tile.WallType == WallID.DirtUnsafe)
						tile.WallType = WallID.GrassUnsafe;
					else if (tile.WallType == WallID.MudUnsafe)
						tile.WallType = WallID.JungleUnsafe;
				}
		}
	}

	public override bool ModifyOpening(ref int x, ref int y, bool isOpening)
	{
		if (!isOpening)
			x += WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(-20, -16) : WorldGen.genRand.Next(15, 21);

		for (int i = x - 40; i < x + 40; ++i)
		{
			for (int j = y - 40; j < y + 40; ++j)
			{
				Tile tile = Main.tile[i, j];
				Tile up = Main.tile[i, j - 1];
				Tile down = Main.tile[i, j + 1];

				if (up.WallType == WallID.None && down.WallType == WallID.None)
					tile.WallType = WallID.None;
				else if (up.WallType != WallID.None && down.WallType != WallID.None)
					tile.WallType = WorldGen.genRand.NextBool() ? down.WallType : up.WallType;
			}
		}

		if (!isOpening)
			Cavinator(x, y, WorldGen.genRand.Next(40, 50));

		return isOpening;
	}

	public static void Cavinator(int i, int j, int steps)
	{
		double offset = WorldGen.genRand.Next(7, 15);
		int horizontalDirection = WorldGen.genRand.NextBool(2) ? -1 : 1;
		Vector2D position = new(i, j);
		Vector2D direction = new(horizontalDirection, WorldGen.genRand.Next(10, 20) * 0.01);
		int repeats = WorldGen.genRand.Next(20, 40);
		HashSet<Point16> grassArea = [];

		FastNoiseLite noise = new();
		noise.SetFrequency(0.03f);

		Dictionary<QuickConversion.BiomeType, float> biases = new() { { QuickConversion.BiomeType.Purity, 0.7f }, { QuickConversion.BiomeType.Jungle, 4 } };
		QuickConversion.BiomeType originalBiome = QuickConversion.FindConversionBiome(new Point16(i - 30, j - 30), new Point16(60, 60), biases);
		GetWallsForBiome(originalBiome, out ushort wall, out ushort altWall);

		while (repeats > 0)
		{
			repeats--;

			int left = (int)Math.Max(position.X - offset * 0.5, 0);
			int right = (int)Math.Min(position.X + offset * 0.5, Main.maxTilesX);
			int top = (int)Math.Max(position.Y - offset * 0.5, 0);
			int bottom = (int)Math.Min(position.Y + offset * 0.5, Main.maxTilesY);

			double fuzz = offset * WorldGen.genRand.Next(80, 120) * 0.01;

			for (int l = top; l < bottom; l++)
			{
				if (l % 10 == 0)
					GetWallsForBiome(QuickConversion.FindConversionBiome(new Point16(i - 30, j - 30), new Point16(60, 60), biases), out wall, out altWall);

				for (int k = left - 2; k < right + 3; k++)
				{
					double xOffset = Math.Abs(k - position.X);
					double yOffset = Math.Abs(l - position.Y);
					double distance = Math.Sqrt(xOffset * xOffset + yOffset * yOffset);
					Tile tile = Main.tile[k, l];

					if (distance < fuzz * 0.4 && TileID.Sets.CanBeClearedDuringGeneration[tile.TileType] && tile.TileType != TileID.Sand)
					{
						if (tile.HasTile)
						{
							for (int x = -1; x < 2; ++x)
							{
								float value = noise.GetNoise(k + x, l);

								if (l < Main.worldSurface + 5 + value * 9)
								{
									Tile wallPlace = Main.tile[k + x, l];
									wallPlace.WallType = value > 0.3f ? altWall : wall;
								}
							}
						}

						if (k > left && k < right)
							tile.HasTile = false;

						for (int x = k - 4; x < k + 5; ++x)
							for (int y = l - 4; y < l + 5; ++y)
								if (originalBiome == QuickConversion.BiomeType.Jungle || y < Main.worldSurface + 30 + noise.GetNoise(k + x, l) * 15)
									grassArea.Add(new Point16(x, y));
					}
				}
			}

			position += direction;
			direction.X = Math.Clamp(direction.X + WorldGen.genRand.Next(-10, 11) * 0.05, horizontalDirection - 0.5, horizontalDirection + 0.5);
			direction.Y = Math.Clamp(direction.Y + WorldGen.genRand.Next(-10, 11) * 0.05, 0, 2);
		}

		foreach (var grass in grassArea)
		{
			for (int x = grass.X - 4; x < grass.X + 5; ++x)
			{
				if (WorldGen.TileIsExposedToAir(x, grass.Y))
				{
					Tile tile = Main.tile[x, grass.Y];

					if (!tile.HasTile)
						continue;

					if (tile.TileType == TileID.Mud && originalBiome == QuickConversion.BiomeType.Jungle)
						tile.TileType = TileID.JungleGrass;
					else if (tile.TileType == TileID.Dirt)
					{
						tile.TileType = originalBiome switch
						{
							QuickConversion.BiomeType.Purity => TileID.Grass,
							QuickConversion.BiomeType.Corruption => TileID.CorruptGrass,
							QuickConversion.BiomeType.Crimson => TileID.CrimsonGrass,
							_ => TileID.Dirt
						};
					}
				}
			}
		}

		if (steps > 0 && position.Y < Main.rockLayer + 50)
			Cavinator((int)position.X, (int)position.Y, steps - 1);
	}

	private static void GetWallsForBiome(QuickConversion.BiomeType biome, out ushort wall, out ushort altWall) => (wall, altWall) = biome switch
	{
		QuickConversion.BiomeType.Purity => (WallID.FlowerUnsafe, WallID.GrassUnsafe),
		QuickConversion.BiomeType.Jungle => (WallID.JungleUnsafe, WallID.MudUnsafe),
		QuickConversion.BiomeType.Ice => (WallID.SnowWallUnsafe, WallID.IceUnsafe),
		QuickConversion.BiomeType.Corruption => (WallID.DirtUnsafe1, WallID.CorruptGrassUnsafe),
		QuickConversion.BiomeType.Crimson => (WallID.DirtUnsafe1, WallID.CrimsonGrassUnsafe),
		_ => (WallID.DirtUnsafe, WallID.DirtUnsafe2)
	};
}
