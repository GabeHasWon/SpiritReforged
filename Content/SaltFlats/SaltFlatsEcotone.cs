using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Common.WorldGeneration.SecretSeeds;
using SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;
using SpiritReforged.Content.SaltFlats.Tiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.Linq;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsEcotone : EcotoneBase
{
	public const int SurfaceNoiseLength = 2;

	[WorldBound]
	public static Rectangle SaltArea;

	public static int AverageY { get; private set; }
	private static FastNoiseLite Noise;

	public override void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries)
	{
		if (tasks.FindIndex(x => x.Name == "Beaches") is int index && index != -1)
			tasks.Insert(index, new PassLegacy("Salt Flats", Generation));
	}

	private static bool CanGenerate(out (int, int) bounds)
	{
		const int offX = EcotoneSurfaceMapping.TransitionLength + 1; //Removes forest patches on the left side

		bounds = (0, 0);
		int spawn = Main.maxTilesX / 2;

		if (SecretSeedSystem.WorldSecretSeed is SaltSeed)
		{
			if (EcotoneSurfaceMapping.FindWhere(OverSpawn) is EcotoneSurfaceMapping.EcotoneEntry entry)
			{
				bounds = (entry.Start.X - offX, entry.End.X);
				return true;
			}
		}
		else if (EcotoneSurfaceMapping.FindWhere("Desert", "Snow", x => !OverSpawn(x)) is EcotoneSurfaceMapping.EcotoneEntry entry) //Uniquely, salt flats cannot normally generate over spawn
		{
			bounds = (entry.Start.X - offX, entry.End.X);
			return true;
		}

		return false;
		bool OverSpawn(EcotoneSurfaceMapping.EcotoneEntry x) => x.Start.X < spawn && x.End.X > spawn;
	}

	private static void Generation(GenerationProgress progress, GameConfiguration configuration)
	{
		if (!CanGenerate(out var bounds))
			return;

		//The strength of the sine for dull salt padding
		const float baseCurveStrength = 5;
		//The base depth of reflective salt padding
		const int baseDepth = 35;
		//The percentage of space surrounding the salt flats that islands can occupy
		const float islandMargin = 0.25f;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		List<Rectangle> islands = [];
		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];
		int fullWidth = xRight - xLeft;

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		//Select the lowest of both neighboring biomes
		AverageY = Math.Max(yLeft, yRight) + 1;

		for (int i = 0; i < 2; i++)
			HillBorder((i == 0) ? xLeft : xRight, i == 0);

		for (int x = xLeft; x < xRight; x++)
		{
			int xProgress = x - xLeft;

			int diminishedDepth = Math.Min(baseDepth, (int)(fullWidth * 0.75f)); //Causes reduced biome depth if it doesn't generate wide enough
			int depth = Math.Min((int)(Math.Sin((float)xProgress / fullWidth * MathHelper.Pi) * (baseCurveStrength * diminishedDepth)), diminishedDepth + (int)(Noise.GetNoise(x, 600) * 8));
			int liningDepth = (int)((8 + (int)(Noise.GetNoise(x, 500) * 6)) /* * ((float)depth / baseDepth)*/);

			int y = (int)(Main.worldSurface * 0.35); //Sky height
			int yMax = AverageY + depth + liningDepth;

			while (y < yMax)
			{
				int type = (y < AverageY + depth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();
				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				if (GetSurfaceY(x) == y && (xProgress < fullWidth * islandMargin || xProgress > fullWidth * (1f - islandMargin)) && WorldGen.genRand.NextBool(50) && !islands.Any(i => i.Contains(new Point(x, y))))
					islands.Add(new(x, y, WorldGen.genRand.Next(14, 21), 3)); //Add island data for later

				if (y == yMax - 1 && !Main.tile[x, y].HasTile) //The final vertical coordinates - fill
				{
					const int fillLimit = 30;
					WorldMethods.ApplyOpenArea((i, j) =>
					{
						if (j > GetSurfaceY(i) && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j)) < fillLimit * fillLimit * 0.1f) //Do a distance check for a naturally rounded fill shape
						{
							var t = Main.tile[i, j];
							if (IsSafe(t))
							{
								t.HasTile = true;
								t.TileType = (ushort)ModContent.TileType<SaltBlockDull>();
								t.Slope = SlopeType.Solid;
							}
						}

						return false;
					}, x, y, new Rectangle(x - fillLimit / 2, y - fillLimit / 2, fillLimit, fillLimit));
				}

				SetTile(x, y++, GetSurfaceY(x), type, type == ModContent.TileType<SaltBlockReflective>());
			}
		}

		AddIslands(islands);

		SaltArea = new Rectangle(xLeft, yLeft - 10, Math.Abs(xRight - xLeft), Math.Abs(yRight - yLeft) + 20);
		WorldDetours.Regions.Add(new(SaltArea, WorldDetours.Context.Piles));
	}

	private static void AddIslands(IEnumerable<Rectangle> areas)
	{
		const int extraWidth = 4;

		foreach (var a in areas)
		{
			int fullWidth = a.Width;

			int startX = a.X - fullWidth / 2;
			int startY = a.Y - 1;

			for (int x = startX; x < startX + fullWidth; x++)
			{
				for (int y = startY; y < startY + 3; y++)
				{
					bool firstLayer = y == startY;
					var tile = Main.tile[x, y];
					int type = firstLayer ? ModContent.TileType<SaltBlockDull>() : ModContent.TileType<SaltBlockReflective>();

					if (!IsSafe(tile))
						continue;

					tile.ClearTile();

					if (!firstLayer || x - startX >= extraWidth && x - startX <= fullWidth - extraWidth)
					{
						tile.TileType = (ushort)type;
						tile.HasTile = true;

						if (firstLayer && (x - startX == extraWidth || x - startX == fullWidth - extraWidth))
							tile.IsHalfBlock = true; //Prevents the Smooth World genpass from interfering later
					}

					if (firstLayer && WorldGen.genRand.NextBool(6))
						Placer.PlaceTile<StoneStupas>(x - 1, y - 1, WorldGen.genRand.Next(0, 12));
				}
			}
		}
	}

	private static void HillBorder(int xCoord, bool left) //Smooths neighboring biome heights using an ease function
	{
		int side = left ? -1 : 1;
		int length = (int)((GetSurfaceY(xCoord) - EcotoneSurfaceMapping.TotalSurfaceY[(short)(xCoord + side)]) * WorldGen.genRand.NextFloat(0.6f, 0.9f));

		if (length < 2)
			return;

		int end = EcotoneSurfaceMapping.TotalSurfaceY[(short)(xCoord + length * side)];

		if (left)
		{
			for (int x = xCoord; x > xCoord - length; x--)
				Clear(x);
		}
		else
		{
			for (int x = xCoord; x < xCoord + length; x++)
				Clear(x);
		}

		void Clear(int x)
		{
			int y = (int)(Main.worldSurface * 0.35); //Sky height
			float progress = (float)(x - xCoord) / length * side;

			int depth = (int)MathHelper.Lerp(GetSurfaceY(x), end, EaseFunction.EaseCircularInOut.Ease(progress));

			while (y < depth)
			{
				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				var tile = Main.tile[x, y];
				if (IsSafe(tile))
				{
					tile.ClearEverything();

					//Clear walls in an expanded area
					WorldMethods.ApplyTileArea(static (x, y) =>
					{
						var tile = Framing.GetTileSafely(x, y);
						if (IsSafe(tile))
							tile.WallType = WallID.None;

						return false;
					}, x - 1, y - 1, x + 1, y + 1);
				}

				y++;
			}
		}
	}

	private static void SetTile(int x, int y, int baseLine, int type, bool clearWall = true)
	{
		var t = Main.tile[x, y];

		if (!IsSafe(t))
			return;

		if (y < baseLine)
		{
			t.ClearEverything();

			if (y == AverageY)
			{
				t.LiquidAmount = 255;
				t.LiquidType = LiquidID.Water;
			}
		}
		else
		{
			t.HasTile = true;
			t.TileType = (ushort)type;
			t.Slope = SlopeType.Solid;

			if (clearWall)
				t.WallType = WallID.None;
		}
	}

	private static bool IsSafe(Tile t)
	{
		int type = t.TileType;
		return (TileID.Sets.GeneralPlacementTiles[type] || type == TileID.Ebonstone || type == TileID.Crimstone) && !SpiritSets.DungeonWall[t.WallType];
	}

	/// <summary> Gets a terrain Y value based on <see cref="AverageY"/>. </summary>
	private static int GetSurfaceY(int x) => AverageY + (int)(Noise.GetNoise(x, 600) * SurfaceNoiseLength);
}