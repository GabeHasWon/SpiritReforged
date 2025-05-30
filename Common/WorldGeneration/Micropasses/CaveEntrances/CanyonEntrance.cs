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
		int depth = Math.Max(80, dif);

		if (depth >= 80)
		{
			int tileY = WorldMethods.FindGround(x, y);
			Tile checkType = Main.tile[x, tileY];

			skipMe = CreateMound(x, y, dif + 10, checkType);
		}

		if (!skipMe)
			DigCavern(x, y, depth + 5);
	}

	private static bool CreateMound(int x, int y, int depth, Tile referenceTile)
	{
		ushort type = referenceTile.TileType switch
		{
			TileID.Mud or TileID.JungleGrass => TileID.Mud,
			TileID.SnowBlock or TileID.IceBlock => TileID.SnowBlock,
			TileID.Sand or TileID.Sandstone or TileID.HardenedSand => TileID.Sand,
			_ => TileID.Dirt,
		};

		if (type == TileID.Sand)
			return true;

		var mound = new Shapes.Mound(WorldGen.genRand.Next(26, 36), depth);
		WorldUtils.Gen(new Point(x, y + depth), mound, Actions.Chain(new Modifiers.Blotches(), new Actions.PlaceTile(type)));
		return false;
	}

	public static void DigCavern(int x, int y, int depth)
	{
		int tileY = WorldMethods.FindGround(x, y);
		Tile checkType = Main.tile[x, tileY];

		(ushort wallDirt, ushort wallStone) = checkType.TileType switch
		{
			TileID.Mud or TileID.JungleGrass => (WallID.MudUnsafe, WallID.JungleUnsafe),
			TileID.SnowBlock or TileID.IceBlock => (WallID.SnowWallUnsafe, WallID.IceUnsafe),
			TileID.Sand or TileID.Sandstone or TileID.HardenedSand => (WallID.HardenedSand, WallID.HardenedSand),
			_ => (WallID.DirtUnsafe, WallID.GrassUnsafe),
		};

		FastNoiseLite diggingNoise = new(WorldGen._genRandSeed);
		diggingNoise.SetFrequency(0.01f);

		FastNoiseLite windingNoise = new(WorldGen._genRandSeed);
		windingNoise.SetFrequency(0.007f);

		FastNoiseLite wallNoise = new(WorldGen._genRandSeed);
		wallNoise.SetFrequency(0.04f);

		for (int j = y; j < y + depth; j++)
		{
			int useX = x - (int)(windingNoise.GetNoise(x, j) * 8);
			int minDistance = (int)MathHelper.Lerp(8, 1, (j - y) / (float)depth);
			int leftEdge = Math.Max(2, (int)(diggingNoise.GetNoise(x + 1200, j) * 4) + minDistance);
			int rightEdge = Math.Max(2, (int)(diggingNoise.GetNoise(x + 2400, j) * 4) + minDistance);

			int wallLeftEdge = (int)(wallNoise.GetNoise(x + 1200, j + 400) * 12) + minDistance;
			int wallRightEdge = (int)(wallNoise.GetNoise(x + 2400, j + 400) * 12) + minDistance;

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

				if (i > useX - wallLeftEdge && i < useX + wallRightEdge)
					tile.Clear(TileDataType.Wall);
				else if ((tile.HasTile || withinTiles) && j > y + 15 && CanFillWalls(i, j))
				{
					float noise = wallNoise.GetNoise(i, j);

					if (noise < 0.16f)
						tile.WallType = wallStone;
					else
						tile.WallType = wallDirt;
				}
			}
		}

		static bool CanFillWalls(int x, int y)
		{
			return Main.tile[x, y].WallType is not WallID.CrimstoneUnsafe or WallID.EbonstoneUnsafe;
		}
	}

	/// <summary>
	/// This makes for a nice shape so I'm keeping it for future reference.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="depth"></param>
	public void WindingCavern(int x, int y, int depth)
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

	public override bool ModifyOpening(ref int x, ref int y, bool isCavinator)
	{
		if (isCavinator)
			return false;

		Generate(x, y); // Replace to KILL the ug desert (and all other structures that may impede)
		y = (int)Main.worldSurface - WorldGen.genRand.Next(5, 30);
		return true;
	}
}
