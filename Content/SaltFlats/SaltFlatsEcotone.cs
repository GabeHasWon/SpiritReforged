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
		public readonly Rectangle Area => new(X - Width / 2, Y - 1, Width, 3);
	}

	private readonly record struct CaveInfo(int X, int Y, int Radius)
	{
		public readonly Rectangle Area => new(X - Radius / 2, Y - Radius / 2, Radius, Radius);
	}

	[WorldBound]
	public static Rectangle SaltArea;

	public static int AverageY { get; private set; }
	private static FastNoiseLite Noise;

	protected override void Load() => TileEvents.OnPlacePot += ConvertPot;

	/// <summary> Converts pots placed atop dull salt into stone stupas. </summary>
	private static bool ConvertPot(int x, int y, ushort type, int style)
	{
		if (WorldGen.generatingWorld)
		{
			var ground = Main.tile[x, y + 1];

			if (ground.HasTile && ground.TileType == ModContent.TileType<SaltBlockDull>())
			{
				WorldGen.PlaceTile(x, y, ModContent.TileType<StoneStupas>(), true, style: WorldGen.genRand.Next(0, 12));
				return false;
			}
		}

		return true;
	}

	public override void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries)
	{
		if (tasks.FindIndex(x => x.Name == "Beaches") is int index && index != -1)
			tasks.Insert(index, new PassLegacy("Salt Flats", Generation));
	}

	private static bool CanGenerate(out (int, int) bounds)
	{
		const int offX = EcotoneSurfaceMapping.TransitionLength + 2; //Removes forest patches on the left side
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

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		List<IslandInfo> islands = [];
		List<CaveInfo> caves = [];
		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];
		int fullWidth = xRight - xLeft;

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		SaltArea = new Rectangle(xLeft, yLeft - 10, Math.Abs(xRight - xLeft), Math.Abs(yRight - yLeft) + 20);
		AverageY = (int)MathHelper.Lerp(yLeft, yRight, 0.5f); //Select the average of both neighboring biomes

		for (int x = xLeft; x < xRight; x++)
		{
			float xProgress = (float)(x - xLeft) / fullWidth;

			int diminishedDepth = Math.Min(baseDepth, (int)(fullWidth * 0.75f)); //Causes reduced depth if the biome doesn't generate wide enough
			float ease = (float)Math.Sin(xProgress * MathHelper.Pi); //Causes tapering around the edges of the biome

			int depthNoise = (int)(Noise.GetNoise(x, 600) * 8);
			int reflectiveDepth = Math.Min((int)(ease * (baseCurveStrength * diminishedDepth)), diminishedDepth + depthNoise);
			int liningDepth = 8 + (int)(Noise.GetNoise(x, 500) * 6);

			int y = (int)(Main.worldSurface * 0.35); //Sky height
			int yMax = AverageY + reflectiveDepth + liningDepth;

			while (y < yMax)
			{
				bool isLining = IsLining(x, y, (float)(y - AverageY) / yMax);
				int surfaceLine = FindSurfaceLine(x, y, yLeft, yRight, isLining);
				int type = (!isLining && y < AverageY + reflectiveDepth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();

				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				if (!isLining && y == surfaceLine)
				{
					MapIsland(new(x, y), xProgress, ref islands);
				}

				if (y == yMax - 1) //The final vertical coordinates - fill
				{
					if (depthNoise < 0)
					{
						MapCave(new(x, y), ref caves); //Occasionally map caves in crests
					}

					if (!Main.tile[x, y].HasTile)
					{
						const int fillLimit = 30;
						WorldMethods.ApplyOpenArea((i, j) =>
						{
							if (j > surfaceLine && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j)) < fillLimit * fillLimit * 0.1f) //Do a distance check for a naturally rounded fill shape
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
				}

				SetTile(x, y++, surfaceLine, type, type == ModContent.TileType<SaltBlockReflective>());
			}
		}

		foreach (var a in islands)
			GenerateIsland(a);

		foreach (var c in caves)
			GenerateCave(c);

		WorldDetours.Regions.Add(new(SaltArea, WorldDetours.Context.Piles));
	}

	private static bool IsLining(int x, int y, float depthProgress)
	{
		//The percentage of space surrounding the biome that will be considered 'lining'
		const float liningWidth = 0.1f;

		float progress = (float)(x - SaltArea.Left) / SaltArea.Width;
		float sine = EaseFunction.EaseSine.Ease((progress - liningWidth) / (1 - liningWidth * 2));
		bool pastLiningWidth = progress is < liningWidth or > (1 - liningWidth);
		bool pastEaseHeight = depthProgress > sine / 3;

		return pastLiningWidth || pastEaseHeight;
	}

	private static int FindSurfaceLine(int x, int y, int yLeft, int yRight, bool isLining)
	{
		//The number of tiles around the biome that can ease into surrounding elevation
		const int mergeDistance = 20;

		int line = AverageY;
		int xStart = x - SaltArea.Left;

		if (xStart < mergeDistance || xStart > SaltArea.Width - mergeDistance) //Merging
		{
			float floatingLine;

			if (xStart < mergeDistance)
				floatingLine = MathHelper.Lerp(yLeft, AverageY, (float)xStart / mergeDistance);
			else
				floatingLine = MathHelper.Lerp(AverageY, yRight, (float)(xStart - (SaltArea.Width - mergeDistance)) / mergeDistance);

			floatingLine += (int)(Noise.GetNoise(x, 600) * 2);
			line = (int)floatingLine;
		}
		else if (isLining) //Lining
		{
			line = AverageY + (int)(Noise.GetNoise(x / 2f, 600) * 2.5f) - 1;
		}

		return line;
	}

	#region features
	/// <param name="coordinates"> The current tile coordinates. </param>
	/// <param name="surfaceProgress"> The progress of the x coordinate along the full width of the biome represented as a range from 0 to 1. </param>
	/// <param name="islands"> The current island data. </param>
	private static void MapIsland(Point coordinates, float surfaceProgress, ref List<IslandInfo> islands)
	{
		//The percentage of space relative to the center of the biome that islands can not occupy
		const float middle = 0.3f;

		int x = coordinates.X;
		int y = coordinates.Y;

		float doubleProgress = (surfaceProgress > 0.5f) ? ((surfaceProgress - 0.5f) / 0.5f) : ((0.5f - surfaceProgress) * 2);
		int chance = (int)Math.Max((1f - doubleProgress) * 100, 1);

		if (doubleProgress > middle && WorldGen.genRand.NextBool(chance) && !islands.Any(i => i.Area.Contains(new Point(x, y))))
		{
			IslandInfo info = new(x, y, WorldGen.genRand.Next(10, 25));

			if (AreaSafe(info.Area))
				islands.Add(info); //Add island data for later
		}
	}

	private static void MapCave(Point coordinates, ref List<CaveInfo> caves)
	{
		if (WorldGen.genRand.NextBool(10))
		{
			int x = coordinates.X;
			int y = coordinates.Y;

			int radius = WorldGen.genRand.Next(3, 9);
			CaveInfo info = new(x, y, radius);

			if (AreaSafe(info.Area))
				caves.Add(info);
		}
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
		WorldUtils.Gen(new(info.X, info.Y), new ModShapes.All(data), new Actions.Custom(AddDecorations));
	}

	private static void GenerateCave(CaveInfo info)
	{
		Point origin = new(info.X, info.Y);

		ShapeData data = new();
		ShapeData outlineData = new();
		ushort saltWall = SpiritReforgedMod.Instance.Find<ModWall>("SaltWallUnsafe").Type;

		WorldUtils.Gen(origin, new Shapes.Slime(info.Radius, WorldGen.genRand.NextFloat(0.5f, 1), WorldGen.genRand.NextFloat(0.5f, 1)), new Actions.ClearTile().Output(data));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.SkipWalls(saltWall),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<SaltBlockDull>())
		).Output(outlineData));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Expand(1),
			new Actions.PlaceWall(saltWall)
		));

		WorldUtils.Gen(origin, new ModShapes.InnerOutline(outlineData), new Actions.Custom(AddDecorations));
	}

	private static bool AddDecorations(int x, int y, object[] args)
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

		if (!WorldGen.SolidTile(x, y + 1))
		{
			return WorldGen.genRand.NextBool(3) && Placer.PlaceTile<SaltStalactite>(x, y + 1).success;
		}

		return false;
	}
	#endregion

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

	private static bool AreaSafe(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = area.Top; y < area.Bottom; y++)
			{
				if (!WorldGen.InWorld(x, y, 8) || !IsSafe(Main.tile[x, y]))
					return false;
			}
		}

		return true;
	}

	private static bool IsSafe(Tile t)
	{
		int type = t.TileType;
		return (TileID.Sets.GeneralPlacementTiles[type] || type == TileID.Ebonstone || type == TileID.Crimstone) && !SpiritSets.DungeonWall[t.WallType];
	}
}