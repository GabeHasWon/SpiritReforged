using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;
using System.Linq;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ZigguratMicropass : Micropass
{
	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Pyramids");
	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int scanRadius = 50;
		const int range = ZigguratBiome.Width / 2;

		Rectangle loc = GenVars.UndergroundDesertLocation;
		Point finalPosition = Point.Zero;

		for (int a = 0; a < 300; a++)
		{
			int rangeLeft = WorldGen.genRand.Next(loc.Left, Math.Max((int)(loc.Center().X - range), loc.Left + 20));
			int rangeRight = WorldGen.genRand.Next(Math.Min((int)(loc.Center().X + range), loc.Right - 20), loc.Right);

			int x = WorldGen.genRand.Next([rangeLeft, rangeRight]);
			int y = loc.Y - 40;

			if (!WorldUtils.Find(new(x, y), new Searches.Down(1500).Conditions(new Conditions.IsSolid()), out Point foundPos))
				return; // ?? big hole where the desert is?

			Point zigguratPos = new(foundPos.X, foundPos.Y + (int)(ZigguratBiome.Height * 0.3f));

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(zigguratPos, new Shapes.Rectangle(new Rectangle(-(ZigguratBiome.Width / 2), -(ZigguratBiome.Height / 2), ZigguratBiome.Width, ZigguratBiome.Height)), new Actions.TileScanner(TileID.Sand, TileID.SandstoneBrick).Output(typeToCount));

			if (typeToCount[TileID.Sand] < scanRadius * scanRadius * 0.5f || typeToCount[TileID.SandstoneBrick] > 10)
				continue;

			CreateDunes(foundPos.X - 80, foundPos.X + 80, foundPos.Y, 10);
			Microbiome.Create<ZigguratBiome>(finalPosition = zigguratPos);

			break;
		}

		int ruinsWidth = ZigguratBiome.Width;
		WorldMethods.Generate(GenerateRuins, WorldGen.genRand.Next(1, 3) * (Main.maxTilesX / WorldGen.WorldSizeSmallX), out _, new(finalPosition.X - ruinsWidth, loc.Y - 40, ruinsWidth * 2, 40), 100);
	}

	public static void CreateDunes(int left, int right, int startY, int duneHeight)
	{
		FastNoiseLite noise = new(WorldGen.genRand.Next());
		noise.SetFrequency(0.03f);

		int y = WorldMethods.FindGround(left, startY);

		for (int x = left; x < right; x++)
		{
			int groundY = WorldMethods.FindGround(x, y);

			if (CanCreateColumn(x, groundY))
			{
				float targetY = groundY - (duneHeight + noise.GetNoise(x, 100) * duneHeight) * EaseFunction.EaseSine.Ease((float)(x - left) / (right - left));
				y = (int)MathHelper.Lerp(y, targetY, 0.3f);

				FillColumn(x, y, TileID.Sand);
			}
			else
			{
				left = x;
			}
		}

		void FillColumn(int x, int top, ushort tileType)
		{
			int y = top;
			int digMax = (int)Math.Max(4f + noise.GetNoise(x, 100) * 8, 1);
			int dig = 0;

			while (WorldGen.InWorld(x, y, 20) && dig < digMax)
			{
				if (WorldGen.SolidTile3(x, y))
					dig++;

				var tile = Main.tile[x, y++];

				if (TileID.Sets.GeneralPlacementTiles[tile.TileType])
					tile.ResetToType(tileType);
			}
		}

		static bool CanCreateColumn(int x, int y) => Main.tile[x, y].TileType != ModContent.TileType<SaltBlockReflective>() && Main.tile[x, y].TileType != ModContent.TileType<SaltBlockDull>();
	}

	#region ruins
	private static bool GenerateRuins(int x, int y)
	{
		if (WorldGen.SolidOrSlopedTile(x, y) || !WorldUtils.Find(new(x, y), new Searches.Down(30).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return false;

		Rectangle structureAreaEstimate = new(foundPos.X - 10, foundPos.Y - 20, 20, 20);
		if (!GenVars.structures.CanPlace(structureAreaEstimate))
			return false;

		Rectangle scanRectangle = new(-10, -10, 20, 20);
		Dictionary<ushort, int> typeToCount = [];
		WorldUtils.Gen(foundPos, new Shapes.Rectangle(scanRectangle), new Actions.TileScanner(TileID.Sand, TileID.SandstoneBrick).Output(typeToCount));

		if (typeToCount[TileID.Sand] < scanRectangle.Width * scanRectangle.Height * 0.5f || typeToCount[TileID.SandstoneBrick] > 5)
			return false;

		Rectangle region = CreateRuin(foundPos.X, foundPos.Y - 10, WorldGen.genRand.Next(2, 5));
		GenVars.structures.AddProtectedStructure(region);
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Walls));
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Piles));

		for (int c = 0; c < WorldGen.genRand.Next(1, 6); c++)
		{
			Point debrisPos = new(foundPos.X + WorldGen.genRand.Next(-20, 20 + 1), foundPos.Y);
			WorldMethods.FindGround(debrisPos.X, ref debrisPos.Y);

			if (Framing.GetTileSafely(debrisPos).WallType == WallID.None)
				CreateSandyDebris(debrisPos.X, debrisPos.Y);
		}

		return true;
	}

	/// <summary> Generates a desert ruin at the provided location with <paramref name="segments"/>. </summary>
	/// <returns> The total area occupied by the ruin. </returns>
	public static Rectangle CreateRuin(int x, int y, int segments)
	{
		Rectangle result = new(Main.maxTilesX, Main.maxTilesY, 0, 0);
		CreateArray(new(x - 4, y - 4, 8, 8), GetRandomDirections(segments), out List<Rectangle> areas);

		foreach (Rectangle a in areas)
			result = new(Math.Min(result.X, a.X), Math.Min(result.Y, a.Y), Math.Max(result.Width, a.Right - result.X), Math.Max(result.Height, a.Bottom - result.Y));

		segments = areas.Count; //Reassign segments to be consistent with our number of predetermined areas
		var shapeData = Enumerable.Repeat(new ShapeData(), segments).ToArray();

		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Location, new Shapes.Rectangle(a.Width, a.Height), Actions.Chain(
				new Actions.PlaceWall((ushort)PolishedSandstoneWall.UnsafeType),
				new Modifiers.RectangleMask(2, a.Width - 2 - 1, 0, a.Height),
				new Actions.PlaceWall(WallID.Sandstone)
			).Output(shapeData[c]));

			ShapeData windowData = new();
			WorldUtils.Gen(a.Location, new ModShapes.All(shapeData[c]), Actions.Chain(
				new Modifiers.RectangleMask(3, a.Width - 3 - 1, 0, a.Height - 3),
				new Actions.PlaceWall((ushort)BronzeGrate.UnsafeType)
			).Output(windowData)); //Add windows

			WorldUtils.Gen(a.Location, new ModShapes.All(windowData), Actions.Chain(
				new Modifiers.Dither(WorldGen.genRand.NextFloat(0.9f)),
				new Actions.ClearWall()
			).Output(windowData)); //Add window dithering

			for (int p = 0; p < 2; p++)
			{
				Point pillarPosition = a.Location + new Point((a.Width - 1) * p, 0);
				WorldUtils.Gen(pillarPosition, new Shapes.Rectangle(1, a.Height), Actions.Chain(
					new Modifiers.IsNotSolid(),
					new Actions.PlaceTile((ushort)ModContent.TileType<RuinedSandstonePillar>())
				));
			}
		} //Generate all segment walls first and collect ShapeData

		ushort[] skipWallTypes = [WallID.Sandstone, (ushort)PolishedSandstoneWall.UnsafeType];
		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Location, new ModShapes.OuterOutline(shapeData[c]), Actions.Chain(
				new Modifiers.SkipWalls(skipWallTypes),
				new Modifiers.IsNotSolid(),
				new Actions.SetTile((ushort)ModContent.TileType<RedSandstoneBrick>())
			));

			for (int p = -1; p < a.Width + 1; p++)
			{
				bool isTile = p < 1 || p >= a.Width - 1;
				int tileType = (p < 1 || p >= a.Width - 1) ? ModContent.TileType<RuinedSandstonePillar>() : -1;
				int wallType = (p < 0 || p >= a.Width) ? WallID.None : RedSandstoneBrickCrackedWall.UnsafeType;
				float ease = 1f - EaseBuilder.EaseSine.Ease((p + 1f) / (float)(a.Width + 1f));

				DropPillar(p + a.X, a.Bottom + 1, tileType, wallType, out int lowestY, (tileType == -1) ? Math.Max((int)(ease * 15), 1) : 0);

				Point basePosition = new(p + a.X, lowestY + 1);
				Tile tile = Framing.GetTileSafely(basePosition);

				if (isTile && tile.HasTileType(TileID.Sand) && WorldGen.TileIsExposedToAir(basePosition.X, basePosition.Y))
					tile.ResetToType((ushort)ModContent.TileType<GildedSandstone>());
			}
		}

		foreach (Rectangle a in areas)
		{
			if (WorldGen.genRand.NextBool(3))
				AddHole(a with { Height = 2 });
		} //Add random holes

		if (WorldGen.genRand.NextBool())
		{
			Point basementPos = new(x, y + 30);
			WorldMethods.FindGround(basementPos.X, ref basementPos.Y);

			CreateHiddenRuin(new(basementPos.X - 4, basementPos.Y, 8, 16));
		} //Add a hidden room underground

		Rectangle decoArea = result; //Add decorations
		decoArea.Inflate(4, 4);
		Decorator decorator = new(decoArea);

		if (WorldGen.genRand.NextBool(3))
			decorator.Enqueue(AddFlagpole, 1);

		decorator.Run();

		return result;
	}

	private static Point[] GetRandomDirections(int length)
	{
		var result = new Point[length];

		for (int c = 0; c < length; c++)
		{
			int unit = WorldGen.genRand.NextFromList(-1, 1);
			result[c] = WorldGen.genRand.NextBool() ? new(unit, 0) : new(0, unit);
		}

		return result;
	}

	private static void CreateArray(Rectangle source, Point[] directions, out List<Rectangle> fullArray)
	{
		fullArray = [];
		for (int c = 0; c < directions.Length; c++)
		{
			source.X += source.Width * directions[c].X;
			source.Y += source.Height * directions[c].Y;

			if (!fullArray.Contains(source))
				fullArray.Add(source); //Avoid duplicates if direction reiterates
		}
	}

	private static void CreateHiddenRuin(Rectangle area)
	{
		ShapeData data = new(); //Create the initial secret room
		WorldUtils.Gen(area.Location, new Shapes.Rectangle(area.Width, area.Height), Actions.Chain(
			new Actions.ClearTile(),
			new Actions.PlaceWall((ushort)PolishedSandstoneWall.UnsafeType),
			new Modifiers.RectangleMask(2, area.Width - 2 - 1, 0, area.Height),
			new Actions.PlaceWall(WallID.Sandstone)
		).Output(data));

		WorldUtils.Gen(area.Location, new ModShapes.All(data), Actions.Chain(
			new Modifiers.RectangleMask(0, area.Width, 0, 5),
			new Actions.PlaceWall((ushort)ModContent.WallType<RedSandstoneBrickForegroundWall>())
		));

		for (int p = 0; p < 2; p++)
		{
			Point pillarPosition = area.Location + new Point((area.Width - 1) * p, 0);
			WorldUtils.Gen(pillarPosition, new Shapes.Rectangle(1, area.Height), Actions.Chain(
				new Modifiers.IsNotSolid(),
				new Actions.PlaceTile((ushort)ModContent.TileType<RuinedSandstonePillar>()),
				new Modifiers.RectangleMask(0, 1, 0, 5),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrick>())
			));
		}

		WorldUtils.Gen(area.Location, new ModShapes.OuterOutline(data), new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrick>()));
		WorldUtils.Gen(area.Location + new Point(2, -2), new Shapes.Rectangle(area.Width - 4, 2), new Actions.ClearTile());

		//Begin an optional extension of the room
		CreateArray(new(area.Center.X - 4, area.Bottom - 8, 8, 8), GetRandomDirections(WorldGen.genRand.Next(5)), out List<Rectangle> areas);

		areas.RemoveAll(NotInSandOrSandstone);
		int segments = areas.Count;
		
		if (segments != 0)
		{
			var shapeData = Enumerable.Repeat(new ShapeData(), segments).ToArray();

			for (int c = 0; c < segments; c++)
			{
				Rectangle a = areas[c];
				WorldUtils.Gen(a.Location, new Shapes.Rectangle(a.Width, a.Height), Actions.Chain(
					new Actions.Clear(),
					new Actions.PlaceWall((ushort)PolishedSandstoneWall.UnsafeType),
					new Modifiers.RectangleMask(2, a.Width - 2 - 1, 0, a.Height),
					new Actions.PlaceWall(WallID.Sandstone)
				).Output(shapeData[c]));

				for (int p = 0; p < 2; p++)
				{
					Point pillarPosition = a.Location + new Point((a.Width - 1) * p, 0);
					WorldUtils.Gen(pillarPosition, new Shapes.Rectangle(1, a.Height), new Actions.PlaceTile((ushort)ModContent.TileType<RuinedSandstonePillar>()));
				}
			} //Generate all segment walls first and collect ShapeData

			ushort[] skipWallTypes = [WallID.Sandstone, (ushort)PolishedSandstoneWall.UnsafeType];
			for (int c = 0; c < segments; c++)
			{
				Rectangle a = areas[c];
				WorldUtils.Gen(a.Location, new ModShapes.OuterOutline(shapeData[c]), Actions.Chain(
					new Modifiers.SkipWalls(skipWallTypes),
					new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrick>())
				));
			}
		}

		static bool NotInSandOrSandstone(Rectangle rectangle)
		{
			int sandyCount = 0;
			for (int x = rectangle.X; x < rectangle.X + rectangle.Width; x++)
			{
				for (int y = rectangle.Y; y < rectangle.Y + rectangle.Height; y++)
				{
					Tile tile = Framing.GetTileSafely(x, y);
					if (tile.HasTileType(TileID.Sand) || tile.HasTileType(TileID.Sandstone) || tile.HasTileType(TileID.HardenedSand))
						sandyCount++;
				}
			}

			return sandyCount < rectangle.Width * rectangle.Height * 0.8f;
		}
	}

	/// <summary> Adds a hole to a random location in <paramref name="area"/>. </summary>
	private static void AddHole(Rectangle area)
	{
		ShapeData data = new();
		var position = WorldGen.genRand.NextVector2FromRectangle(area).ToPoint();

		WorldUtils.Gen(position, new Shapes.Circle(WorldGen.genRand.Next(1, 4)), Actions.Chain(
			new Actions.Clear(),
			new Modifiers.Expand(WorldGen.genRand.Next(1, 3)),
			new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrickCracked>()),
			new Modifiers.Dither(),
			new Actions.ClearTile()
		).Output(data));

		WorldUtils.Gen(position, new ModShapes.OuterOutline(data), new Actions.Custom(PlaceFloorSand));

		static bool PlaceFloorSand(int x, int y, object args)
		{
			Tile tile = Main.tile[x, y];
			Tile above = Framing.GetTileSafely(x, y - 1);
			Tile below = Framing.GetTileSafely(x, y + 1);

			if (WorldGen.SolidOrSlopedTile(tile) && WorldGen.SolidOrSlopedTile(below) && !WorldGen.SolidOrSlopedTile(above))
			{
				tile.ResetToType(TileID.Sand);
				return true;
			}

			return false;
		}
	}

	private static bool AddFlagpole(int x, int y)
	{
		int height = WorldGen.genRand.Next(1, 4);
		int flagDepth = Math.Min(WorldGen.genRand.Next(0, 2), height - 1);
		bool result = false;

		if (Main.tile[x, y].WallType == WallID.None && Framing.GetTileSafely(x, y + 1).HasTileType(ModContent.TileType<RedSandstoneBrick>()))
		{
			for (int i = 0; i < height; i++)
			{
				PlaceAttempt attempt = Placer.PlaceTile<FlagRing>(x, y - i);
				result |= attempt.success;

				if (i == height - 1 - flagDepth)
				{
					Main.tile[x, y - i].TileFrameY = FlagRing.SlopeFrame;

					FlagRing.FlagRingEntity entity = ModContent.GetInstance<FlagRing.FlagRingEntity>();
					entity.Hook_AfterPlacement(x, y - i, entity.Type, 0, 0, 0);
				}
			}
		}

		return result;
	}

	private static void DropPillar(int x, int y, int tileType, int wallType, out int lowestY, int length = 0)
	{
		lowestY = y;
		int currentLength = 0;

		while ((length == 0 || currentLength < length) && WorldGen.InWorld(x, y, 20) && !WorldGen.SolidOrSlopedTile(x, y) && Main.tile[x, y] is Tile tile && tile.WallType == WallID.None)
		{
			if (wallType != WallID.None)
				tile.WallType = (ushort)wallType;
			if (tileType != -1)
				WorldGen.PlaceTile(x, y, tileType, true);

			lowestY = y;
			currentLength++;
			y++;
		}
	}

	public static void CreateSandyDebris(int x, int y)
	{
		int width = WorldGen.genRand.Next(3, 7);

		WorldUtils.Gen(new(x, y), new Shapes.Tail(width, new(-1, -WorldGen.genRand.Next(2, 5))), new Actions.SetTileKeepWall(TileID.Sandstone));
		WorldUtils.Gen(new(x, y), new Shapes.Tail(width, new(-1, 4)), Actions.Chain(
			new Actions.SetTileKeepWall(TileID.Sandstone),
			new Modifiers.Expand(1),
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.SetTileKeepWall(TileID.HardenedSand)
		));

		if (WorldGen.genRand.NextBool(3))
		{
			WorldUtils.Gen(new(x, y - 5), new Shapes.Rectangle(2, 5), Actions.Chain(
				new Modifiers.IsNotSolid(),
				new Modifiers.SkipWalls(),
				new Actions.PlaceTile((ushort)ModContent.TileType<RuinedSandstonePillar>())
			));
		}
	}
	#endregion
}