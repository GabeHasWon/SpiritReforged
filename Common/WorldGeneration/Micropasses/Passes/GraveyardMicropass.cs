using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Misc;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.SaltFlats.Tiles;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class GraveyardMicropass : Micropass
{
	private enum StructureSize { Small, Medium, Large }

	public override string WorldGenName => "Graveyard";

	/// <summary> The scale of this graveyard, reset after worldgen. </summary>
	[WorldBound]
	public static float Scale;

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Piles"));

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		WorldMethods.Generate(GenerateGraveyard, 1, out int generated, new(100, (int)(Main.worldSurface * 0.35f), Main.maxTilesX - 200, 10));

		if (generated == 0)
			SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: " + WorldGenName);
	}

	public static bool GenerateGraveyard(int x, int y)
	{
		const int dimensions = 60;
		WorldMethods.FindGround(x, ref y);

		Scale = Math.Clamp(Math.Abs(x - Main.maxTilesX / 2f) / (Main.maxTilesX / 2f) * 1.3f, 0, 1); //Determine graveyard size based on distance from the world center
		Rectangle region = new(x - dimensions / 2, y - dimensions / 6, dimensions, dimensions / 3);

		if (!GenVars.structures.CanPlace(region, 4))
			return false;

		Dictionary<ushort, int> typeToCount = [];
		WorldUtils.Gen(region.Location, new Shapes.Rectangle(region.Width, region.Height), new Actions.TileScanner(TileID.Grass, TileID.Dirt).Output(typeToCount));
		int totalScan = region.Width * region.Height;

		if (typeToCount[TileID.Grass] + typeToCount[TileID.Dirt] < totalScan * 0.3f || typeToCount[TileID.Grass] < 5)
			return false;

		Decorator decorator = new(region);

		if ((int)(3 * Scale) is int platformCount && platformCount > 0)
			decorator.Enqueue(CreatePlatform, platformCount);

		if ((int)(WorldGen.genRand.Next(5, 9) * Scale) is int graveCount && graveCount > 0)
			decorator.Enqueue(PlaceGravestone, graveCount);

		if ((int)(WorldGen.genRand.Next(3, 8) * Scale) is int thornCount && thornCount > 0)
			decorator.Enqueue(GrowThorns, thornCount);

		if ((int)(WorldGen.genRand.Next(3, 8) * Scale) is int fillerCount && fillerCount > 0)
		{
			decorator.Enqueue(PlaceFences, fillerCount);
			decorator.Enqueue(GrowBushes, fillerCount);
		}

		if (Scale > 0.5f)
			decorator.Enqueue(CreateMausoleum, 1);
		else
			decorator.Enqueue(PlaceNamedGrave, 1);

		decorator.Run();
		region.Inflate(8, 8);

		GenVars.structures.AddProtectedStructure(region);
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Piles | WorldDetours.Context.Walls));

		return true;
	}

	private static bool GrowThorns(int x, int y)
	{
		bool success = false;
		Tile surfaceTile = Framing.GetTileSafely(x, y + 1);

		if (WorldGen.SolidTile(surfaceTile) && surfaceTile.TileType is TileID.Grass or TileID.Dirt)
		{
			int length = WorldGen.genRand.Next(3, 13);
			int type = ModContent.TileType<GiantThorns>();
			Point position = new(x, y);

			for (int c = 0; c < length; c++)
			{
				Tile tile = Framing.GetTileSafely(position);

				if (WorldGen.SolidOrSlopedTile(tile))
					break;

				WorldGen.PlaceTile(position.X, position.Y, type);

				if (!tile.HasTile || tile.TileType != type)
					break;

				bool horizontal = WorldGen.genRand.NextBool();
				position += new Point(horizontal ? WorldGen.genRand.NextFromList(-1, 1) : 0, horizontal ? 0 : WorldGen.genRand.NextFromList(-1, 1));
				success = true;
			}
		}

		return success;
	}

	private static bool PlaceFences(int x, int y)
	{
		Tile surfaceTile = Framing.GetTileSafely(x, y + 1);

		if (WorldGen.SolidTile(surfaceTile) && surfaceTile.TileType is TileID.Grass or TileID.Dirt)
		{
			WorldUtils.Gen(new(x, y), new Shapes.Circle(5), Actions.Chain(
				new Modifiers.IsSolid(),
				new Modifiers.IsTouchingAir(),
				new Modifiers.Offset(0, -1),
				new Modifiers.OnlyWalls(WallID.None),
				new Actions.PlaceWall(WallID.WroughtIronFence)
			));

			return true;
		}

		return false;
	}

	private static bool GrowBushes(int x, int y)
	{
		Tile surfaceTile = Framing.GetTileSafely(x, y + 1);

		if (WorldGen.SolidTile(surfaceTile) && surfaceTile.TileType is TileID.Grass or TileID.Dirt)
		{
			WorldUtils.Gen(new(x, y), new Shapes.Circle(WorldGen.genRand.Next(2, 8)), Actions.Chain(
				new Modifiers.IsSolid(),
				new Modifiers.IsTouchingAir(),
				new Modifiers.Offset(0, -1),
				new Modifiers.Blotches(3, 0.5),
				new Actions.PlaceWall(WallID.GrassUnsafe)
			));

			return true;
		}

		return false;
	}

	private static bool PlaceGravestone(int x, int y)
	{
		if (WorldGen.IsTileNearby(x, y, TileID.Tombstones, 4))
			return false;

		int style = WorldGen.genRand.Next(6); //Select any non-gold variant
		int surfaceType = Framing.GetTileSafely(x, y + 1).TileType;

		if (surfaceType is TileID.Sand)
			style = WorldGen.genRand.Next(6, 11); //Select gold variants
		else if (surfaceType is TileID.SnowBlock or TileID.IceBlock)
			style = WorldGen.genRand.NextFromList(0, 2, 3, 5); //Avoid mossy variants

		return Placer.PlaceTile(x, y, TileID.Tombstones, style).success;
	}

	private static bool PlaceNamedGrave(int x, int y)
	{
		Rectangle area = new(x - 1, y - 1, 3, 2);
		if (Main.tile[x, y].LiquidAmount != 255 && GenVars.structures.CanPlace(area))
		{
			PlaceAttempt tombstone = Placer.Check(x, y, TileID.Tombstones, WorldGen.genRand.Next(5));
			PlaceAttempt skeleton = Placer.Check(x - 1, y, ModContent.TileType<SkeletonHand>());

			if (tombstone.success && skeleton.success)
			{
				tombstone.Place();
				skeleton.Place();

				Sign.TextSign(Sign.ReadSign(x, y - 1), Language.GetTextValue("Mods.SpiritReforged.Misc.GraveText"));
				WorldGen.PlaceTile(x - 1, y - 1, ModContent.TileType<SkeletonHand>(), true, true, style: WorldGen.genRand.Next(3));

				GenVars.structures.AddProtectedStructure(area);
				return true;
			}
		}

		return false;
	}

	private static bool CreatePlatform(int x, int y)
	{
		Tile tile = Main.tile[x, y];

		if (!WorldGen.SolidTile(tile))
		{
			if (!WorldUtils.Find(new(x, y), new Searches.Down(12).Conditions(new Conditions.IsSolid()), out Point surfacePos))
				return false;

			int radius = WorldGen.genRand.Next(6, 13);
			ShapeData data = new();

			WorldUtils.Gen(new(x, y), new Shapes.Slime(radius, 1, 0.4), Actions.Chain(
				new Modifiers.Flip(false, true),
				new Actions.SetTileKeepWall(TileID.Dirt)
			).Output(data));

			WorldUtils.Gen(new(x, y), new ModShapes.OuterOutline(data), Actions.Chain(
				new Modifiers.RectangleMask(-radius - 2, radius + 2, 0, radius),
				new Modifiers.Dither(),
				new Actions.ClearTile()
			));

			WorldUtils.Gen(new(x, y), new ModShapes.InnerOutline(data), Actions.Chain(
				new Actions.SetTileKeepWall(TileID.Grass),
				new Actions.Custom(SmoothTop)
			));

			int fullWallWidth = radius * 2 - 6;
			WorldUtils.Gen(new(x - fullWallWidth / 2, y + 1), new Shapes.Rectangle(fullWallWidth, surfacePos.Y - y + 4), Actions.Chain(
				new Modifiers.IsTouchingAir(),
				new Modifiers.Blotches(),
				new Actions.PlaceWall(WallID.GrassUnsafe),
				new Modifiers.Expand(1),
				new Modifiers.Dither(),
				new Actions.PlaceWall(WallID.GrassUnsafe)
			));

			return true;
		}

		return false;

		static bool SmoothTop(int x, int y, object args)
		{
			Tile top = Framing.GetTileSafely(x, y - 1);
			Tile left = Framing.GetTileSafely(x - 1, y);
			Tile right = Framing.GetTileSafely(x + 1, y);
			Tile tile = Main.tile[x, y];

			if (!WorldGen.SolidOrSlopedTile(top))
			{
				bool leftClear = !WorldGen.SolidOrSlopedTile(left);
				bool rightClear = !WorldGen.SolidOrSlopedTile(right);

				if (leftClear && rightClear)
					tile.IsHalfBlock = true;
				else if (leftClear)
					tile.Slope = SlopeType.SlopeDownRight;
				else if (rightClear)
					tile.Slope = SlopeType.SlopeDownLeft;

				return true;
			}

			return false;
		}
	}

	private static bool CreateMausoleum(int x, int y)
	{
		var size = (StructureSize)((Scale - 0.5f) / 0.25f);

		WorldMethods.FindGround(x, ref y);
		y -= 2;

		Point origin = new(x, y);
		Rectangle topArea = (size is StructureSize.Large) ? new(x - 8, y - 6, 16, 6) : new(x - 7, y - 5, 14, 5);
		ShapeData data = new();

		WorldUtils.Gen(topArea.Location, new Shapes.Rectangle(topArea.Width, topArea.Height), 
			new Actions.Clear()); //Clear the top area

		WorldUtils.Gen(origin, new Shapes.Rectangle(new(-(topArea.Width / 2), 0, topArea.Width, 1)), 
			new Actions.SetTile(TileID.StoneSlab).Output(data)); //Stone slab foundation

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Offset(0, 1),
			new Actions.SetTile(TileID.GrayBrick),
			new Modifiers.RectangleMask(-5, 5, 0, 1),
			new Actions.PlaceWall(WallID.GrayBrick)
		)); //Bottom gray brick foundation

		WorldUtils.Gen(origin, new Shapes.Rectangle(new(-(topArea.Width / 2), -topArea.Height - 3, topArea.Width, 3)), Actions.Chain(
			new Actions.SetTile((ushort)ModContent.TileType<BrownShingles>()),
			new Modifiers.Offset(0, 1),
			new Modifiers.SkipTiles((ushort)ModContent.TileType<BrownShingles>()),
			new Actions.SetTile(TileID.StoneSlab)
		)); //Flat shingle roof

		CreateTriangleRoof(new(origin.X, origin.Y - topArea.Height - 2), 6, 4);

		WorldUtils.Gen(origin, new Shapes.Rectangle(new(-(topArea.Width / 2) + 1, -topArea.Height + 1, 2, topArea.Height - 1)), Actions.Chain(
			new Actions.SetTile((ushort)ModContent.TileType<StonePillar>()),
			new Modifiers.Offset(1, 0),
			new Modifiers.Expand(0, 1),
			new Actions.PlaceWall(WallID.StoneSlab)
		)); //Left stone pillar

		WorldUtils.Gen(origin, new Shapes.Rectangle(new(topArea.Width / 2 - 3, -topArea.Height + 1, 2, topArea.Height - 1)), Actions.Chain(
			new Actions.SetTile((ushort)ModContent.TileType<StonePillar>()),
			new Modifiers.Offset(-1, 0),
			new Modifiers.Expand(0, 1),
			new Actions.PlaceWall(WallID.StoneSlab)
		)); //Right stone pillar

		Rectangle bottomArea = new(x - (topArea.Width / 2 - 2), y + 2, topArea.Width - 4, 3);

		WorldUtils.Gen(bottomArea.Location, new Shapes.Rectangle(bottomArea.Width, bottomArea.Height * 2), Actions.Chain(
			new Modifiers.Expand(1),
			new Actions.SetTileKeepWall(TileID.GrayBrick)
		)); //Create foundation

		if (size is StructureSize.Large)
		{
			WorldUtils.Gen(bottomArea.Location + new Point(1, 0), new Shapes.Rectangle(bottomArea.Width - 2, bottomArea.Height), Actions.Chain(
				new Actions.Clear(),
				new Actions.PlaceWall(WallID.StoneSlab)
			)); //Clear basement area

			CreateBonePit(origin + new Point(0, 5), 6, 4);

			WorldUtils.Gen(origin, new Shapes.Rectangle(new(-2, -1, 4, 1)), 
				new Actions.PlaceWall(WallID.WroughtIronFence));

			WorldUtils.Gen(origin, new Shapes.Rectangle(new(-(topArea.Width / 2) + 4, 0, 2, 3)), 
				new Actions.ClearTile()); //Create openings

			WorldUtils.Gen(origin, new Shapes.Rectangle(new(topArea.Width / 2 - 6, 0, 2, 3)), 
				new Actions.ClearTile());
		}
		else
		{
			CreateBonePit(origin, 6, 4);
		}

		return true;

		static void CreateBonePit(Point origin, int width, int depth)
		{
			ShapeData data = new();
			Rectangle bounds = new(origin.X - width / 2, origin.Y, width, depth);

			WorldUtils.Gen(bounds.Location, new Shapes.Rectangle(bounds.Width, bounds.Height), Actions.Chain(
				new Actions.Clear(),
				new Actions.PlaceWall(WallID.StoneSlab)
			).Output(data));

			WorldUtils.Gen(bounds.Location, new ModShapes.OuterOutline(data), Actions.Chain(
				new Modifiers.RectangleMask(-bounds.Width, bounds.Width, 2, bounds.Height),
				new Actions.SetTileKeepWall(TileID.StoneSlab)
			));

			WorldUtils.Gen(bounds.Location, new ModShapes.All(data), Actions.Chain(
				new Modifiers.RectangleMask(-bounds.Width, bounds.Width, 1, bounds.Height),
				new Modifiers.Blotches(),
				new Actions.PlaceTile((ushort)ModContent.TileType<BonePile>())
			));

			Point center = bounds.Center;
			for (int p = 0; p < 5; p++)
			{
				if (Placer.PlaceTile<SkeletonHand>(center.X, center.Y - p).success)
					break;
			}
		}

		static void CreateTriangleRoof(Point origin, int squareSize, int thickness)
		{
			Rectangle bounds = new(origin.X - squareSize / 2, origin.Y - squareSize / 2, squareSize, squareSize / 2);
			int halfThickness = thickness / 2;

			WorldUtils.Gen(bounds.Location, new Shapes.Rectangle(bounds.Width, bounds.Height * 2), new Actions.ClearTile());

			for (int y = -halfThickness; y < halfThickness; y++)
			{
				Utils.TileActionAttempt attempt = (y >= halfThickness - 1) ? PlaceSlabs : PlaceShingles;

				Utils.PlotLine(bounds.BottomLeft().ToPoint() + new Point(0, y), bounds.Top().ToPoint() + new Point(0, y), attempt);
				Utils.PlotLine(bounds.BottomRight().ToPoint() + new Point(-1, y), bounds.Top().ToPoint() + new Point(-1, y), attempt);
			}

			WorldUtils.Gen(bounds.Location, new Shapes.Rectangle(bounds.Width, bounds.Height * 2), new Actions.Smooth());
		}

		static bool PlaceShingles(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			tile.ResetToType((ushort)ModContent.TileType<BrownShingles>());

			return true;
		}

		static bool PlaceSlabs(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			tile.ResetToType(TileID.StoneSlab);

			return true;
		}
	}
}