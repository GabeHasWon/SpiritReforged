using SpiritReforged.Common.WorldGeneration.Noise;
using System.Runtime.InteropServices;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;

internal class CanyonEntrance : CaveEntrance
{
	private static bool LateGeneration = false;

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
		QuickConversion.BiomeType biome = QuickConversion.FindConversionBiome(new Point16(x - 40, y - 40), new Point16(80, 140), biases);
		bool clear = false;

		if (biome == QuickConversion.BiomeType.Desert)
		{
			if (!LateGeneration)
				return true;

			clear = true;
			depth /= 2;
		}

		ushort type = biome switch
		{
			QuickConversion.BiomeType.Jungle => TileID.Mud,
			QuickConversion.BiomeType.Ice => TileID.SnowBlock,
			QuickConversion.BiomeType.Crimson => TileID.Crimstone,
			QuickConversion.BiomeType.Corruption => TileID.Ebonstone,
			QuickConversion.BiomeType.Desert => TileID.Sand,
			_ => TileID.Dirt,
		};

		var mound = new Shapes.Mound(WorldGen.genRand.Next(46, 56), depth);
		GenAction action = clear
			? Actions.Chain(new Modifiers.Blotches(), new Modifiers.Conditions(new Conditions.IsTile(TileID.Dirt)), new Actions.Clear())
			: Actions.Chain(new Modifiers.Blotches(), new Actions.PlaceTile(type));

		WorldUtils.Gen(new Point(x, y + depth), mound, action);
		return false;
	}

	public static void DigCavern(int x, int y, int depth)
	{
		int tileY = WorldMethods.FindGround(x, y);
		Dictionary<QuickConversion.BiomeType, float> biases = new() { { QuickConversion.BiomeType.Purity, 0.7f }, { QuickConversion.BiomeType.Jungle, 4 },
			{ QuickConversion.BiomeType.Desert, 4 } };
		QuickConversion.BiomeType biome = QuickConversion.FindConversionBiome(new Point16(x - 40, tileY - 40), new Point16(80, 140), biases);
		bool desertClear = false;

		if (biome == QuickConversion.BiomeType.Desert)
		{
			if (!LateGeneration)
				return;

			desertClear = true;
		}

		(ushort wallDirt, ushort wallStone) = biome switch
		{
			QuickConversion.BiomeType.Jungle => (WallID.MudUnsafe, WallID.MudUnsafe),
			QuickConversion.BiomeType.Ice => (WallID.IceUnsafe, WallID.IceUnsafe),
			QuickConversion.BiomeType.Corruption => (WallID.CorruptGrassUnsafe, WallID.DirtUnsafe1),
			QuickConversion.BiomeType.Crimson => (WallID.CrimsonGrassUnsafe, WallID.DirtUnsafe1),
			QuickConversion.BiomeType.Desert => (WallID.Sandstone, WallID.HardenedSand),
			_ => (WallID.FlowerUnsafe, WallID.JungleUnsafe3),
		};

		HashSet<Point16> runners = [];
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

				if (!desertClear)
				{
					if (canClear && withinTiles)
						tile.Clear(TileDataType.Tile);
					else if (WorldGen.genRand.NextBool(240))
						runners.Add(new Point16(i, j));
				}

				if ((tile.HasTile && !WorldGen.TileIsExposedToAir(i, j) || withinTiles) && j > y + 8 + wallNoise.GetNoise(i, j) * 6)
				{
					float noise = wallNoise.GetNoise(i, j);

					if (desertClear && tile.WallType is not WallID.Sandstone and not WallID.HardenedSand)
					{
						tile.WallType = WallID.None;
					}
					else
					{
						if (noise < 0.3f)
							tile.WallType = wallDirt;
						else
							tile.WallType = wallStone;
					}
				}
			}
		}

		if (biome is QuickConversion.BiomeType.Ice or QuickConversion.BiomeType.Purity)
		{
			foreach (var point in runners)
			{
				WorldGen.TileRunner(point.X, point.Y, WorldGen.genRand.NextFloat(4, 13), 8, biome == QuickConversion.BiomeType.Purity ? TileID.Stone : TileID.IceBlock);
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

		LateGeneration = true;
		Generate(x, y); // Replace to KILL the ug desert (and all other structures that may impede)
		LateGeneration = false;
		y = (int)Main.worldSurface - WorldGen.genRand.Next(5, 30);
		return true;
	}
}
