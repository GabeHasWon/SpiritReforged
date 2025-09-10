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
	private readonly record struct IslandInfo(int X, int Y, int Width)
	{
		public readonly bool Contains(Point point) => new Rectangle(X - Width / 2, Y - 1, Width, 3).Contains(point);
	}

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

		if (SecretSeedSystem.WorldSecretSeed is SaltSeed)
		{
			if (EcotoneSurfaceMapping.FindWhere(EcotoneSurfaceMapping.OverSpawn) is EcotoneSurfaceMapping.EcotoneEntry entry)
			{
				bounds = (entry.Start.X - offX, entry.End.X);
				return true;
			}
		}
		else if (EcotoneSurfaceMapping.FindWhere(x => x.SurroundedBy("Desert", "Snow") && !EcotoneSurfaceMapping.OverSpawn(x) && EcotoneSurfaceMapping.OnSurface(x)) is EcotoneSurfaceMapping.EcotoneEntry entry)
		{
			bounds = (entry.Start.X - offX, entry.End.X);
			return true; //Uniquely, salt flats cannot normally generate over spawn
		}

		return false;
	}

	private static void Generation(GenerationProgress progress, GameConfiguration configuration)
	{
		if (!CanGenerate(out var bounds))
			return;

		//The strength of the sine for dull salt padding
		const float baseCurveStrength = 5;
		//The base depth of reflective salt padding
		const int baseDepth = 35;
		//The number of blocks lining the biome that will be replaced with dull salt
		const int dullLiningWidth = 5;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		List<IslandInfo> islands = [];
		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];
		int fullWidth = xRight - xLeft;

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		AverageY = Math.Max(yLeft, yRight) + 1; //Select the lowest of both neighboring biomes

		for (int i = 0; i < 2; i++)
			HillBorder((i == 0) ? xLeft : xRight, i == 0);

		for (int x = xLeft; x < xRight; x++)
		{
			float xProgress = (float)(x - xLeft) / fullWidth;

			int diminishedDepth = Math.Min(baseDepth, (int)(fullWidth * 0.75f)); //Causes reduced depth if the biome doesn't generate wide enough
			float ease = (float)Math.Sin(xProgress * MathHelper.Pi);

			int finalDepth = Math.Min((int)(ease * (baseCurveStrength * diminishedDepth)), diminishedDepth + (int)(Noise.GetNoise(x, 600) * 8));
			int liningDepth = 8 + (int)(Noise.GetNoise(x, 500) * 6);

			int y = (int)(Main.worldSurface * 0.35); //Sky height
			int yMax = AverageY + finalDepth + liningDepth;

			while (y < yMax)
			{
				bool isLining = x < xLeft + dullLiningWidth || x > xRight - dullLiningWidth;
				int type = (!isLining && y < AverageY + finalDepth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();

				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				if (!isLining)
				{
					MapIsland(new(x, y), xProgress, ref islands);
				}

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

				SetTile(x, y++, isLining ? (GetSurfaceY(x) - 1) : GetSurfaceY(x), type, type == ModContent.TileType<SaltBlockReflective>());
			}
		}

		foreach (var a in islands)
			GenerateIsland(a);

		SaltArea = new Rectangle(xLeft, yLeft - 10, Math.Abs(xRight - xLeft), Math.Abs(yRight - yLeft) + 20);
		WorldDetours.Regions.Add(new(SaltArea, WorldDetours.Context.Piles));
	}

	#region features
	/// <param name="coordinates"> The current tile coordinates. </param>
	/// <param name="surfaceProgress"> The progress of the x coordinate along the full width of the biome represented as a range from 0 to 1. </param>
	/// <param name="islands"> The current island data. </param>
	private static void MapIsland(Point coordinates, float surfaceProgress, ref List<IslandInfo> islands)
	{
		//The percentage of space relative to the center of the biome that islands can not occupy
		const float middle = 0.7f;

		int x = coordinates.X;
		int y = coordinates.Y;

		float doubleProgress = (surfaceProgress > 0.5f) ? ((surfaceProgress - 0.5f) / 0.5f) : ((0.5f - surfaceProgress) * 2);

		if (GetSurfaceY(x) == y && doubleProgress > middle && WorldGen.genRand.NextBool(7) && !islands.Any(i => i.Contains(new Point(x, y))))
			islands.Add(new(x, y, WorldGen.genRand.Next(10, 25))); //Add island data for later
	}

	private static void GenerateIsland(IslandInfo info)
	{
		ShapeData data = new();
		int halfWidth = info.Width / 2;

		WorldUtils.Gen(new(info.X, info.Y), new Shapes.Circle(halfWidth, WorldGen.genRand.Next(1, 4)), Actions.Chain(
			new Modifiers.SkipTiles((ushort)ModContent.TileType<SaltBlockReflective>()),
			new Modifiers.RectangleMask(-halfWidth, halfWidth, -2, 2),
			new Actions.SetTile((ushort)ModContent.TileType<SaltBlockDull>())
		).Output(data));

		WorldUtils.Gen(new(info.X, info.Y), new ModShapes.InnerOutline(data), new Actions.Smooth());
		WorldUtils.Gen(new(info.X, info.Y), new ModShapes.All(data), new Actions.Custom(static (x, y, args) =>
		{
			if (!WorldGen.SolidTile(x, y - 1))
			{
				bool success = false;

				if (WorldGen.genRand.NextBool(6))
					success |= Placer.PlaceTile<StoneStupas>(x - 1, y - 1, WorldGen.genRand.Next(0, 12)).success;

				if (WorldGen.genRand.NextBool(3))
					success |= Placer.PlaceTile<Saltwort>(x, y - 1).success;

				return success;
			}

			return false;
		}));
	}
	#endregion

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
	private static int GetSurfaceY(int x) => AverageY /*+ (int)(Noise.GetNoise(x / 5f, 600) * 2)*/;
}