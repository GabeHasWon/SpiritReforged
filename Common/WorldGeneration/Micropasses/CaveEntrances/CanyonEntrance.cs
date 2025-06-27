using SpiritReforged.Common.WorldGeneration.Noise;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;

internal class CanyonEntrance : CaveEntrance
{
	public override CaveEntranceType Type => CaveEntranceType.Canyon;

	public override void Generate(int x, int y)
	{
		y -= 2;

		bool skipMe = false;

		if (y > Main.worldSurface - 80)
			y = (int)Main.worldSurface - 80;

		int dif = (int)Main.worldSurface - y;
		int depth = Math.Max(140, dif);

		if (depth >= 140)
			skipMe = CreateMound(x, y, dif + 10);

		if (!skipMe)
			DigCavern(x, y, depth + 5);
	}

	private static bool CreateMound(int x, int y, int depth)
	{
		Dictionary<QuickConversion.BiomeType, float> biases = new() { { QuickConversion.BiomeType.Purity, 0.7f }, { QuickConversion.BiomeType.Jungle, 4 }, 
			{ QuickConversion.BiomeType.Desert, 4 } };
		QuickConversion.BiomeType biome = QuickConversion.FindConversionBiome(new Point16(x - 40, y - 40), new Point16(80, 80), biases);

		if (biome == QuickConversion.BiomeType.Desert)
			return true;

		ushort type = biome switch
		{
			QuickConversion.BiomeType.Jungle => TileID.Mud,
			QuickConversion.BiomeType.Ice => TileID.SnowBlock,
			QuickConversion.BiomeType.Crimson => TileID.Crimstone,
			QuickConversion.BiomeType.Corruption => TileID.Ebonstone,
			_ => TileID.Dirt,
		};

		var mound = new Shapes.Mound(WorldGen.genRand.Next(46, 56), depth);
		WorldUtils.Gen(new Point(x, y + depth), mound, Actions.Chain(new Modifiers.Blotches(), new Actions.PlaceTile(type)));
		return false;
	}

	public static void DigCavern(int x, int y, int depth)
	{
		int tileY = WorldMethods.FindGround(x, y);
		Dictionary<QuickConversion.BiomeType, float> biases = new() { { QuickConversion.BiomeType.Purity, 0.7f }, { QuickConversion.BiomeType.Jungle, 4 },
			{ QuickConversion.BiomeType.Desert, 4 } };
		QuickConversion.BiomeType biome = QuickConversion.FindConversionBiome(new Point16(x - 40, tileY - 40), new Point16(80, 80),	biases);

		if (biome == QuickConversion.BiomeType.Desert)
			return;

		(ushort wallDirt, ushort wallStone) = biome switch
		{
			QuickConversion.BiomeType.Jungle => (WallID.MudUnsafe, WallID.JungleUnsafe2),
			QuickConversion.BiomeType.Ice => (WallID.SnowWallUnsafe, WallID.IceUnsafe),
			QuickConversion.BiomeType.Corruption => (WallID.CorruptGrassUnsafe, WallID.DirtUnsafe1),
			QuickConversion.BiomeType.Crimson => (WallID.CrimsonGrassUnsafe, WallID.DirtUnsafe1),
			_ => (WallID.GrassUnsafe, WallID.FlowerUnsafe),
		};

		FastNoiseLite diggingNoise = new(WorldGen._genRandSeed);
		diggingNoise.SetFrequency(0.01f);

		FastNoiseLite windingNoise = new(WorldGen._genRandSeed);
		windingNoise.SetFrequency(0.007f);

		FastNoiseLite wallNoise = new(WorldGen._genRandSeed);
		wallNoise.SetFrequency(0.03f);

		for (int j = y; j < y + depth; j++)
		{
			int useX = x - (int)(windingNoise.GetNoise(x, j) * 8);
			int minDistance = (int)MathHelper.Lerp(8, 1, (j - y) / (float)depth);
			int leftEdge = Math.Max(2, (int)(diggingNoise.GetNoise(x + 1200, j) * 4) + minDistance);
			int rightEdge = Math.Max(2, (int)(diggingNoise.GetNoise(x + 2400, j) * 4) + minDistance);

			int left = useX - leftEdge * 2;
			int right = useX + rightEdge * 2;

			for (int i = left; i < right; ++i)
			{
				Tile tile = Main.tile[i, j];

				// Ice and snow are in the can't be cleared set because they're dumb, ignore that
				bool canClear = TileID.Sets.CanBeClearedDuringGeneration[tile.TileType] || tile.TileType is TileID.SnowBlock or TileID.IceBlock;
				bool withinTiles = i >= useX - leftEdge && i <= useX + rightEdge;

				if (canClear && withinTiles)
					tile.Clear(TileDataType.Tile);

				if ((tile.HasTile && !WorldGen.TileIsExposedToAir(i, j) || withinTiles) && j > y + 8 + wallNoise.GetNoise(i, j) * 6)
				{
					float noise = wallNoise.GetNoise(i, j);

					if (noise < 0.3f)
						tile.WallType = wallDirt;
					else
						tile.WallType = wallStone;
				}
			}
		}
	}

	/// <summary>
	/// This makes for a nice shape so I'm keeping it for future reference.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="depth"></param>
	public static void WindingCavern(int x, int y, int depth)
	{
		FastNoiseLite diggingNoise = new(WorldGen._genRandSeed);
		diggingNoise.SetFrequency(0.1f);

		FastNoiseLite windingNoise = new(WorldGen._genRandSeed);
		windingNoise.SetFrequency(0.03f);

		for (int j = y; j < y + depth; j++)
		{
			int useX = x - (int)(windingNoise.GetNoise(x, j) * 24);
			int minDistance = (int)MathHelper.Lerp(8, 1, (j - y) / (float)depth);
			int leftEdge = (int)(diggingNoise.GetNoise(x + 1200, j) * 4) + minDistance;
			int rightEdge = (int)(diggingNoise.GetNoise(x + 2400, j) * 4) + minDistance;

			for (int i = useX - leftEdge; i < useX + rightEdge; ++i)
			{
				Tile tile = Main.tile[i, j];

				tile.ClearEverything();
			}
		}
	}

	public override bool ModifyOpening(ref int x, ref int y, bool isOpening)
	{
		if (isOpening)
			return false;

		Generate(x, y); // Replace to KILL the ug desert (and all other structures that may impede)
		y = (int)Main.worldSurface - WorldGen.genRand.Next(5, 30);
		return true;
	}
}
