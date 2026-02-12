using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Forest.Graveyard;
using SpiritReforged.Content.Forest.Walls;
using SpiritReforged.Content.SaltFlats.Tiles;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class GraveyardMicropass : Micropass
{
	/// <summary> Used to conveniently store mausoleum structure tile types between static methods. </summary>
	private readonly record struct MausoleumTilePalette
	{
		public const int TileSlab = 0;
		public const int TileBrick = 1;
		public const int TileColumn = 2;

		public const int WallSlab = 3;
		public const int WallBrick = 4;

		public readonly ushort[] Types = new ushort[5];

		public MausoleumTilePalette(QuickConversion.BiomeType biome)
		{
			if (biome is QuickConversion.BiomeType.Desert)
			{
				Types[TileSlab] = (ushort)ModContent.TileType<RedSandstoneBrick>();
				Types[TileBrick] = TileID.SandstoneBrick;
				Types[TileColumn] = (ushort)ModContent.TileType<RuinedSandstonePillar>();

				Types[WallSlab] = WallID.Sandstone;
				Types[WallBrick] = WallID.SandstoneBrick;
			}
			else
			{
				Types[TileSlab] = TileID.StoneSlab;
				Types[TileBrick] = TileID.GrayBrick;
				Types[TileColumn] = (ushort)ModContent.TileType<StonePillar>();

				Types[WallSlab] = WallID.StoneSlab;
				Types[WallBrick] = WallID.GrayBrick;
			}
		}
	}

	private enum StructureSize { Small, Medium, Large }

	public override string WorldGenName => "Graveyard";

	/// <summary> The scale of this graveyard, reset after worldgen. </summary>
	[WorldBound]
	public static float Scale;
	/// <summary> The native biome of this graveyard, reset after worldgen. </summary>
	[WorldBound]
	public static QuickConversion.BiomeType Biome;
	private static MausoleumTilePalette MausoleumPalette;

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
		WorldUtils.Gen(region.Location, new Shapes.Rectangle(region.Width, region.Height), new Actions.TileScanner(TileID.SnowBlock, TileID.IceBlock, TileID.Sand, TileID.Grass, TileID.Dirt).Output(typeToCount));
		int totalScan = region.Width * region.Height;

		if (Scale > 0.5f && typeToCount[TileID.SnowBlock] + typeToCount[TileID.IceBlock] > totalScan * 0.5f)
		{
			Biome = QuickConversion.BiomeType.Ice;
		}
		else if (Scale > 0.5f && typeToCount[TileID.Sand] > totalScan * 0.5f)
		{
			Biome = QuickConversion.BiomeType.Desert;
		}
		else if (typeToCount[TileID.Grass] + typeToCount[TileID.Dirt] > totalScan * 0.3f && typeToCount[TileID.Grass] >= 5)
		{
			Biome = QuickConversion.BiomeType.Purity;
		}
		else
		{
			return false;
		}

		int namedGraveIndex = -1;
		Decorator decorator = new(region);

		if ((int)(3 * Scale) is int platformCount && platformCount > 0)
			decorator.Enqueue(CreatePlatform, platformCount);

		if ((int)(9 * Scale) is int graveCount && graveCount > 0)
			decorator.Enqueue(PlaceGravestone, graveCount);

		if ((int)(WorldGen.genRand.Next(3, 8) * Scale) is int fillerCount && fillerCount > 0)
		{
			decorator.Enqueue(PlaceFences, fillerCount);

			if (Biome is QuickConversion.BiomeType.Purity)
				decorator.Enqueue(GrowBushes, fillerCount);
		}

		if (Biome is QuickConversion.BiomeType.Purity && (int)(5 * Scale) is int thornCount && thornCount > 0)
			decorator.Enqueue(GrowThorns, thornCount);

		if (Scale > 0.5f)
			decorator.Enqueue(CreateMausoleum, 1);
		else
			namedGraveIndex = decorator.Enqueue(PlaceNamedGrave, 1).LastIndex;

		decorator.Run(out int[] objectCounts);

		if (namedGraveIndex != -1 && objectCounts[namedGraveIndex] == 0) //PlaceNamedGrave failed to complete
			new Decorator(region).Enqueue(CreateCaveClearing, 1).Run(out _);

		region.Inflate(8, 8);
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Piles | WorldDetours.Context.Walls));

		return true;
	}

	private static bool GrowThorns(int x, int y)
	{
		bool success = false;
		Tile surfaceTile = Framing.GetTileSafely(x, y + 1);

		if (WorldGen.SolidTile(surfaceTile) && surfaceTile.TileType is TileID.Grass or TileID.Dirt)
		{
			success |= GrowThornBush(x, y, WorldGen.genRand.Next(3, 10), false);
			success |= GrowThornBush(x, y, 6, false);

			if (success)
				GrowThornBush(x, y, WorldGen.genRand.Next(3, 10), true);
		}

		return success;

		static bool GrowThornBush(int x, int y, int length, bool background)
		{
			bool success = false;
			int type = ModContent.TileType<GiantThorns>();
			Point position = new(x, y);

			for (int c = 0; c < length; c++)
			{
				Tile tile = Framing.GetTileSafely(position);

				if (WorldGen.SolidOrSlopedTile(tile))
					break;

				if (background)
				{
					if (tile.WallType == WallID.None)
					{
						tile.WallType = (ushort)ModContent.WallType<GiantThornWall>();

						if (c == 0)
							Framing.GetTileSafely(x, y + 1).WallType = (ushort)ModContent.WallType<GiantThornWall>();
					}
				}
				else
				{
					WorldGen.PlaceTile(position.X, position.Y, type);

					if (!tile.HasTile || tile.TileType != type)
						break;
				}

				bool horizontal = WorldGen.genRand.NextBool();
				position += new Point(horizontal ? WorldGen.genRand.NextFromList(-1, 1) : 0, horizontal ? 0 : -1);
				success = true;
			}

			return success;
		}
	}

	private static bool PlaceFences(int x, int y)
	{
		Tile surfaceTile = Framing.GetTileSafely(x, y + 1);

		if (WorldGen.SolidTile(surfaceTile))
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
		if (WorldGen.IsTileNearby(x, y, TileID.Tombstones, 3))
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

	private static bool CreateCaveClearing(int x, int y)
	{
		Rectangle fullArea = new(x, y, 8, 6);
		if (!WorldMethods.AreaClear(fullArea))
		{
			ShapeData data = new();
			WorldUtils.Gen(new(x + fullArea.Width / 2, y + fullArea.Height), new Shapes.Mound(fullArea.Width / 2, fullArea.Height), Actions.Chain(
				new Actions.ClearTile(),
				new Modifiers.Expand(1),
				new Actions.PlaceWall(WallID.GrassUnsafe)
			).Output(data));

			WorldUtils.Gen(new(x + fullArea.Width / 2, y + fullArea.Height), new ModShapes.OuterOutline(data), new Actions.Smooth());

			PlaceNamedGrave(x + 4, y + 6);
			Placer.PlaceTile<LightShaft>(x + 4, y + 1);

			return true;
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

			if (Biome is QuickConversion.BiomeType.Desert)
			{
				WorldUtils.Gen(new(x, y), new Shapes.Mound(radius, radius), Actions.Chain(
					new Modifiers.Flip(false, true),
					new Actions.SetTileKeepWall(TileID.Sand)
				).Output(data));

				WorldUtils.Gen(new(x, y), new ModShapes.All(data), Actions.Chain(
					new Modifiers.Offset(0, 2),
					new Modifiers.SkipTiles(TileID.Sand),
					new Actions.SetTileKeepWall(TileID.Sandstone)
				));
			}
			else
			{
				WorldUtils.Gen(new(x, y), new Shapes.Slime(radius, 1, 0.4), Actions.Chain(
					new Modifiers.Flip(false, true),
					new Actions.SetTileKeepWall((Biome is QuickConversion.BiomeType.Ice) ? TileID.SnowBlock : TileID.Dirt)
				).Output(data));

				WorldUtils.Gen(new(x, y), new ModShapes.OuterOutline(data), Actions.Chain(
					new Modifiers.RectangleMask(-radius - 2, radius + 2, 0, radius),
					new Modifiers.Dither(),
					new Actions.ClearTile()
				));
			}

			GenAction outlineAction = Actions.Chain((Biome is QuickConversion.BiomeType.Purity) ? [new Actions.SetTileKeepWall(TileID.Grass), new Actions.Custom(SmoothTop)] : [new Actions.Custom(SmoothTop)]);
			WorldUtils.Gen(new(x, y), new ModShapes.InnerOutline(data), outlineAction);

			int fullWallWidth = radius * 2 - 6;
			ushort wallType = Biome switch
			{
				QuickConversion.BiomeType.Ice => WallID.SnowWallUnsafe,
				QuickConversion.BiomeType.Desert => WallID.Sandstone,
				_ => WallID.GrassUnsafe
			};

			WorldUtils.Gen(new(x - fullWallWidth / 2, y + 1), new Shapes.Rectangle(fullWallWidth, surfacePos.Y - y + 4), Actions.Chain(
				new Modifiers.IsTouchingAir(),
				new Modifiers.Blotches(),
				new Actions.PlaceWall(wallType),
				new Modifiers.Expand(1),
				new Modifiers.Dither(),
				new Actions.PlaceWall(wallType)
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

	#region mausoleum
	private static bool CreateMausoleum(int x, int y)
	{
		if (!WorldUtils.Find(new(x, y), new Searches.Down(12).Conditions(new Conditions.IsSolid()), out Point surfacePos) || !WorldMethods.AreaClear(x, y = surfacePos.Y - 8, 8, 8))
			return false;

		MausoleumPalette = new(Biome);
		var size = (StructureSize)((Scale - 0.5f) / 0.25f);

		Rectangle topArea = (size is StructureSize.Large) ? new(x - 8, y - 6, 16, 6) : new(x - 7, y - 5, 14, 5);
		Point bottom = new(x, y);
		ShapeData data = new();

		WorldUtils.Gen(topArea.Location, new Shapes.Rectangle(topArea.Width, topArea.Height), 
			new Actions.Clear()); //Clear the top area

		WorldUtils.Gen(bottom, new Shapes.Rectangle(new(-(topArea.Width / 2), 0, topArea.Width, 1)), 
			new Actions.SetTile(MausoleumPalette.Types[MausoleumTilePalette.TileSlab]).Output(data)); //Stone slab foundation

		WorldUtils.Gen(bottom, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Offset(0, 1),
			new Actions.SetTile(MausoleumPalette.Types[MausoleumTilePalette.TileBrick]),
			new Modifiers.RectangleMask(-5, 5, 0, 1),
			new Actions.PlaceWall(MausoleumPalette.Types[MausoleumTilePalette.WallBrick])
		)); //Bottom gray brick foundation

		CreateWholeRoof(topArea);

		WorldUtils.Gen(bottom, new Shapes.Rectangle(new(-(topArea.Width / 2) + 1, -topArea.Height + 1, 2, topArea.Height - 1)), Actions.Chain(
			new Actions.SetTileKeepWall(MausoleumPalette.Types[MausoleumTilePalette.TileColumn]),
			new Modifiers.Offset(1, 0),
			new Modifiers.Expand(0, 1),
			new Actions.PlaceWall(MausoleumPalette.Types[MausoleumTilePalette.WallSlab])
		)); //Left stone pillar

		WorldUtils.Gen(bottom, new Shapes.Rectangle(new(topArea.Width / 2 - 3, -topArea.Height + 1, 2, topArea.Height - 1)), Actions.Chain(
			new Actions.SetTileKeepWall(MausoleumPalette.Types[MausoleumTilePalette.TileColumn]),
			new Modifiers.Offset(-1, 0),
			new Modifiers.Expand(0, 1),
			new Actions.PlaceWall(MausoleumPalette.Types[MausoleumTilePalette.WallSlab])
		)); //Right stone pillar

		Rectangle bottomArea = new(x - (topArea.Width / 2 - 2), y + 2, topArea.Width - 4, 3);

		WorldUtils.Gen(bottomArea.Location, new Shapes.Rectangle(bottomArea.Width, (int)(bottomArea.Height * 2.5f)), Actions.Chain(
			new Modifiers.Expand(1),
			new Actions.SetTileKeepWall(MausoleumPalette.Types[MausoleumTilePalette.TileBrick])
		)); //Create foundation

		if (size is StructureSize.Large)
		{
			WorldUtils.Gen(bottomArea.Location + new Point(1, 0), new Shapes.Rectangle(bottomArea.Width - 2, bottomArea.Height), Actions.Chain(
				new Actions.Clear(),
				new Actions.PlaceWall(MausoleumPalette.Types[MausoleumTilePalette.WallSlab])
			)); //Clear basement area

			CreateSkeletonPit(bottom + new Point(0, 5), 6, 4);

			if (Biome is QuickConversion.BiomeType.Desert)
			{
				WorldUtils.Gen(topArea.Location + new Point(3, 1), new Shapes.Rectangle(topArea.Width - 5, topArea.Height), Actions.Chain(
					new Modifiers.OnlyWalls(WallID.None),
					new Actions.PlaceWall((ushort)BronzeGrate.UnsafeType),
					new Modifiers.Offset(0, -1),
					new Modifiers.OnlyWalls(WallID.None),
					new Actions.PlaceWall(WallID.Sandstone)
				));
			}
			else
			{
				WorldUtils.Gen(bottom, new Shapes.Rectangle(new(-2, -1, 4, 1)), new Actions.PlaceWall(WallID.WroughtIronFence));
			}

			WorldUtils.Gen(bottom, new Shapes.Rectangle(new(-(topArea.Width / 2) + 4, 0, 2, 3)), new Actions.ClearTile()); //Create left opening
			WorldUtils.Gen(bottom, new Shapes.Rectangle(new(topArea.Width / 2 - 6, 0, 2, 3)), new Actions.ClearTile()); //Create right opening

			if (Biome is QuickConversion.BiomeType.Desert)
			{
				WorldUtils.Gen(bottom, new Shapes.Rectangle(new(-(topArea.Width / 2), 0, topArea.Width, 1)), Actions.Chain(
					new Modifiers.IsNotSolid(),
					new Actions.SetTileKeepWall((ushort)ModContent.TileType<BronzeGrateBlock>())
				));
			}
		}
		else
		{
			CreateSkeletonPit(bottom, 6, 4);
		}

		return true;
	}

	private static void CreateWholeRoof(Rectangle area)
	{
		ShapeData data = new();
		Point origin = area.Center;

		WorldUtils.Gen(origin, new Shapes.Rectangle(new(-(area.Width / 2), -area.Height, area.Width, 3)), Actions.Chain(
			new Actions.SetTile((ushort)ModContent.TileType<BrownShingles>()),
			new Modifiers.Offset(0, 1),
			new Modifiers.SkipTiles((ushort)ModContent.TileType<BrownShingles>()),
			new Actions.SetTile(MausoleumPalette.Types[MausoleumTilePalette.TileSlab])
		).Output(data)); //Flat shingle roof

		if (Biome is QuickConversion.BiomeType.Purity)
		{
			WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
				new Modifiers.Offset(0, -1),
				new Modifiers.SkipTiles((ushort)ModContent.TileType<BrownShingles>()),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<LimeMoss>())
			)); //Add moss
		}

		const int squareSize = 6;
		const int thickness = 4;

		Rectangle bounds = new(origin.X - squareSize / 2, origin.Y - squareSize / 2 - area.Height + 1, squareSize, squareSize / 2);
		int halfThickness = thickness / 2;

		WorldUtils.Gen(bounds.Location, new Shapes.Rectangle(bounds.Width, bounds.Height * 2), new Actions.ClearTile());

		for (int y = -halfThickness; y < halfThickness; y++)
		{
			Utils.TileActionAttempt attempt = (y >= halfThickness - 1) ? PlaceSlabs : PlaceShingles;

			Utils.PlotLine(bounds.BottomLeft().ToPoint() + new Point(0, y), bounds.Top().ToPoint() + new Point(0, y), attempt);
			Utils.PlotLine(bounds.BottomRight().ToPoint() + new Point(-1, y), bounds.Top().ToPoint() + new Point(-1, y), attempt);
		}

		WorldUtils.Gen(bounds.Location, new Shapes.Rectangle(bounds.Width, bounds.Height * 2), new Actions.Smooth());

		static bool PlaceShingles(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			tile.ResetToType((ushort)ModContent.TileType<BrownShingles>());

			return true;
		}

		static bool PlaceSlabs(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			tile.ResetToType(MausoleumPalette.Types[MausoleumTilePalette.TileSlab]);

			return true;
		}
	}

	private static void CreateSkeletonPit(Point origin, int width, int depth)
	{
		ShapeData data = new();
		Rectangle bounds = new(origin.X - width / 2, origin.Y, width, depth);

		WorldUtils.Gen(bounds.Location, new Shapes.Rectangle(bounds.Width, bounds.Height), Actions.Chain(
			new Actions.Clear(),
			new Actions.PlaceWall(MausoleumPalette.Types[MausoleumTilePalette.WallSlab])
		).Output(data));

		WorldUtils.Gen(bounds.Location, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.RectangleMask(-bounds.Width, bounds.Width, 2, bounds.Height),
			new Actions.SetTileKeepWall(MausoleumPalette.Types[MausoleumTilePalette.TileSlab])
		));

		WorldUtils.Gen(bounds.Location, new ModShapes.All(data), Actions.Chain(
			new Modifiers.RectangleMask(-bounds.Width, bounds.Width, 1, bounds.Height),
			new Modifiers.Blotches(),
			new Modifiers.IsNotSolid(),
			new Actions.PlaceTile((ushort)ModContent.TileType<BonePile>())
		));

		Point center = bounds.Center;
		for (int p = 0; p < 5; p++)
		{
			if (Placer.PlaceTile<SkeletonHand>(center.X, center.Y - p).success)
				break;
		}
	}
	#endregion
}