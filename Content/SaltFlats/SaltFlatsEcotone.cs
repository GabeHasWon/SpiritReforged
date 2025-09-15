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
using SpiritReforged.Content.SaltFlats.Walls;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsEcotone : EcotoneBase
{
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
		const int baseDepth = 30;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		List<CaveInfo> caves = [];
		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];
		int fullWidth = xRight - xLeft;

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		SaltArea = new Rectangle(xLeft, Math.Min(yLeft, yRight) - 5, Math.Abs(xRight - xLeft), Math.Abs(yRight - yLeft) + baseDepth + 20);
		AverageY = (int)MathHelper.Lerp(yLeft, yRight, 0.5f); //Select the average of both neighboring biomes

		for (int x = xLeft; x < xRight; x++)
		{
			float xProgress = (float)(x - xLeft) / fullWidth;
			float ease = EaseFunction.EaseSine.Ease(xProgress); //Causes tapering around the edges of the biome

			int depthNoise = (int)(Noise.GetNoise(x, 600) * 8);
			int reflectiveDepth = Math.Min((int)(ease * (baseCurveStrength * baseDepth)), baseDepth + depthNoise);
			int liningDepth = 8 + (int)(Noise.GetNoise(x, 500) * 6);

			int y = (int)(Main.worldSurface * 0.35); //Sky height
			int yMax = AverageY + reflectiveDepth + liningDepth;

			while (y < yMax)
			{
				bool isLining = IsLining(x, y, (float)(y - AverageY) / yMax);
				int surfaceLine = FindSurfaceLine(x, y, yLeft, yRight, isLining);
				int type = (!isLining && y >= AverageY && y < AverageY + reflectiveDepth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();

				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
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

				int wallType = (type == ModContent.TileType<SaltBlockDull>() && !WorldGen.TileIsExposedToAir(x, y)) ? SaltWall.UnsafeType : WallID.None;
				SetTile(x, y++, surfaceLine, type, wallType);
			}
		}

		foreach (var c in caves)
			GenerateCave(c);

		Decorate();

		WorldDetours.Regions.Add(new(SaltArea, WorldDetours.Context.Piles));
	}

	private static bool IsLining(int x, int y, float depthProgress)
	{
		//The percentage of space surrounding the biome that will be considered 'lining'
		float liningWidth = Math.Clamp(EaseFunction.EaseSine.Ease(Noise.GetNoise(0.5f, 0.5f)), 0.1f, 0.2f);

		float progress = (float)(x - SaltArea.Left) / SaltArea.Width;
		float sine = EaseFunction.EaseSine.Ease((progress - liningWidth) / (1 - liningWidth * 2));
		bool pastLiningWidth = progress < liningWidth || progress > 1 - liningWidth;
		bool pastEaseHeight = depthProgress > sine / 3;

		return pastLiningWidth || pastEaseHeight;
	}

	private static int FindSurfaceLine(int x, int y, int yLeft, int yRight, bool isLining)
	{
		//The number of tiles around the biome that can ease into surrounding elevation
		const int mergeDistance = 30;
		//The number of visible steps for merging
		const float steps = 5;

		float surfaceNoise = Noise.GetNoise(x, 100) * 2;
		int xStart = x - SaltArea.Left;

		if (xStart < mergeDistance || xStart > SaltArea.Width - mergeDistance) //Merging
		{
			float floatingLine;

			if (xStart < mergeDistance)
				floatingLine = MathHelper.Lerp(yLeft, AverageY, (int)(xStart / steps) * steps / mergeDistance);
			else
				floatingLine = MathHelper.Lerp(AverageY, yRight, (int)((xStart - (SaltArea.Width - mergeDistance)) / steps) * steps / mergeDistance);

			return (int)(floatingLine + surfaceNoise);
		}
		else if (isLining) //Lining
		{
			return (int)(AverageY + surfaceNoise) - 1;
		}
		else //Center
		{
			float surfaceProgress = (float)(x - SaltArea.Left) / SaltArea.Width;
			float doubleProgress = Math.Max(EaseFunction.EaseSine.Ease(surfaceProgress * 3), 0);

			return (int)Math.Min(AverageY + surfaceNoise * 2f * doubleProgress, AverageY);
		}
	}

	#region features
	private static void Decorate() => WorldMethods.GenerateSquared(static (i, j) =>
	{
		var tile = Main.tile[i, j];
		if (tile.HasTile && tile.TileType == ModContent.TileType<SaltBlockDull>())
		{
			bool leftEmpty = !WorldGen.SolidTile(i - 1, j);
			bool rightEmpty = !WorldGen.SolidTile(i + 1, j);

			if (!Main.tile[i, j - 1].HasTile && (leftEmpty || rightEmpty))
			{
				tile.Clear(Terraria.DataStructures.TileDataType.Slope);
				if (WorldGen.genRand.NextBool(4))
				{
					tile.IsHalfBlock = true;
				}
				else
				{
					SlopeType slope = leftEmpty ? SlopeType.SlopeDownRight : SlopeType.SlopeDownLeft;
					tile.Slope = slope;
				}

				return false;
			}

			if (!WorldGen.SolidTile(i, j - 1))
			{
				if (WorldGen.genRand.NextBool(6))
					Placer.PlaceTile<StoneStupas>(i - 1, j - 1, WorldGen.genRand.Next(0, 12));

				if (WorldGen.genRand.NextBool(2))
					Placer.PlaceTile<Saltwort>(i, j - 1);
			}

			if (!WorldGen.SolidTile(i, j + 1) && WorldGen.genRand.NextBool(6))
				Placer.PlaceTile<SaltStalactite>(i, j + 1);
		}

		return false;
	}, out _, SaltArea);

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

	private static void GenerateCave(CaveInfo info)
	{
		Point origin = new(info.X, info.Y);

		ShapeData data = new();
		ShapeData outlineData = new();

		WorldUtils.Gen(origin, new Shapes.Slime(info.Radius, WorldGen.genRand.NextFloat(0.5f, 1), WorldGen.genRand.NextFloat(0.5f, 1)), new Actions.ClearTile().Output(data));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.SkipWalls((ushort)SaltWall.UnsafeType),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<SaltBlockDull>())
		).Output(outlineData));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Expand(1),
			new Actions.PlaceWall((ushort)SaltWall.UnsafeType)
		));
	}
	#endregion

	private static void SetTile(int x, int y, int baseLine, int type, int wallType = -1)
	{
		var t = Main.tile[x, y];

		if (!IsSafe(t))
			return;

		if (y < baseLine)
		{
			t.ClearEverything();
		}
		else
		{
			t.HasTile = true;
			t.TileType = (ushort)type;
			t.Slope = SlopeType.Solid;

			if (wallType != -1)
			{
				t.WallType = (ushort)wallType;
			}
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