using SpiritReforged.Content.Forest.ButterflyStaff;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ButterflyMicropass : Micropass
{
	public override string WorldGenName => "Butterfly Shrines";

	// Remnants will take care of our butterfly shrines on their end at some point, change in the future
	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxTries = 2000;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Butterfly");
		int repeats = Main.maxTilesX / WorldGen.WorldSizeSmallX; // 1 shrine in small and medium worlds, 2 in large
		int overallTries = 0;

		for (int i = 0; i < repeats; i++)
		{
			if (overallTries > 15000)
				break;

			Point16 size = new(35, 35); // 35 chosen as an approximation of how big one cavern usually is
			Point16 position = Point16.Zero;
			int tries = 0;

			while (tries < maxTries)
			{
				int third = Main.maxTilesX / 3;
				int x = WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(GenVars.leftBeachEnd, third) : WorldGen.genRand.Next(Main.maxTilesX - third, GenVars.rightBeachStart);
				int y = (int)GenVars.worldSurface + WorldGen.genRand.Next(50, 100);

				position = new Point16(x, y);

				if (SolidPerimeter(new Rectangle(position.X, position.Y, size.X, size.Y)))
					break;

				tries++;
			}

			if (tries == maxTries)
			{
				SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine");
				return; //Failed
			}

			overallTries++;

			if (GenVars.structures.CanPlace(new Rectangle(position.X, position.Y, size.X, size.Y), 4))
			{
				HashSet<QuickConversion.BiomeType> blacklist = [QuickConversion.BiomeType.Jungle, QuickConversion.BiomeType.Mushroom, QuickConversion.BiomeType.Ice];

				if (!WorldGen.remixWorldGen)
					blacklist.Add(QuickConversion.BiomeType.Desert);

				var biome = QuickConversion.FindConversionBiome(position, size);
	
				if (blacklist.Contains(biome))
				{
					i--;
					continue;
				}

				PlaceButterflyGrove(new Point(position.X + size.X / 2, position.Y));
				GenVars.structures.AddProtectedStructure(new Rectangle(position.X, position.Y, size.X, size.Y), 4);
				ButterflySystem.ButterflyZones.Add(new Rectangle(position.X, position.Y, size.X, size.Y));
				StructureTools.ClearActuators(position.X, position.Y, size.X, size.Y);

				var origin = new Point(position.X + size.X / 2, position.Y + 8); //Centered position
				bool foundClearing = WorldUtils.Find(origin, Searches.Chain(new Searches.Up(1000), new Conditions.IsSolid().AreaOr(1, 50).Not()), out var top);
				top.Y += 50;

				if (foundClearing) //Generate a shaft like sword shrines do
				{
					var data = new ShapeData();
					ushort[] ignore = [TileID.LivingWood, TileID.LeafBlock, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick];

					//Fill sand-type walls
					WorldUtils.Gen(new Point(origin.X - 1, top.Y + 10), new Shapes.Rectangle(3, origin.Y - top.Y - 9), Actions.Chain(new Modifiers.Blotches(2, 0.2), new Modifiers.OnlyTiles(TileID.Sand, TileID.HardenedSand, TileID.Sandstone), new Actions.PlaceWall(WallID.HardenedSand)));

					WorldUtils.Gen(new Point(origin.X, top.Y + 10), new Shapes.Rectangle(1, origin.Y - top.Y - 9), Actions.Chain(new Modifiers.Blotches(2, 0.2), new Modifiers.SkipTiles(ignore), new Actions.ClearTile().Output(data), new Modifiers.Expand(1), new Modifiers.OnlyTiles(53), new Actions.SetTile(397).Output(data)));
					WorldUtils.Gen(new Point(origin.X, top.Y + 10), new ModShapes.All(data), new Actions.SetFrames(frameNeighbors: true));
				}
			}
			else
				i--;
		}
	}

	static bool SolidPerimeter(Rectangle area) //Scan the perimeter of 'area' for solid and dirt tiles
	{
		const float solidMargin = .9f, dirtMargin = .5f; //at least 90% solid and 50% dirt
		int solidCount = 0, dirtCount = 0;

		for (int a = 0; a < 2; a++)
		{
			for (int x = area.X; x < area.X + area.Width; x++)
			{
				int y = area.Y + area.Height * a;
				if (WorldGen.SolidTile(x, y))
				{
					solidCount++;

					if (Main.tile[x, y].TileType == TileID.Dirt)
						dirtCount++;
				}
			}
		}

		for (int a = 0; a < 2; a++)
		{
			for (int y = area.Y; y < area.Y + area.Height; y++)
			{
				int x = area.X + area.Width * a;
				if (WorldGen.SolidTile(x, y))
				{
					solidCount++;

					if (Main.tile[x, y].TileType == TileID.Dirt)
						dirtCount++;
				}
			}
		}

		int totalCount = area.Width * 2 + area.Height * 2;
		return solidCount / (float)totalCount >= solidMargin && dirtCount / (float)totalCount >= dirtMargin;
	}

	public static void PlaceButterflyGrove(Point origin)
	{
		ShapeData slimeShapeData = new();
		ShapeData sideCarversShapeData = new();
		Point point = new(origin.X, origin.Y + 20);
		float xScale = 0.8f + WorldGen.genRand.NextFloat() * 0.25f; // Randomize the width of the shrine area

		// Create a masking layer for the cavern, so the walls tilt inwards while going up
		// The masking layer is comprised of two circles, offset left and right respectively
		int maskOffset = 30;
		WorldUtils.Gen(point, new Shapes.Circle(15), Actions.Chain(
			new Modifiers.Offset(maskOffset, -10),
			new Actions.Blank().Output(sideCarversShapeData)
		));

		WorldUtils.Gen(point, new Shapes.Circle(15), Actions.Chain(
			new Modifiers.Offset(-maskOffset, -10),
			new Actions.Blank().Output(sideCarversShapeData)
		));

		// Using the Slime shape, clear out tiles. Accomodate for the side carvers mask, to create a nice bell shape
		WorldUtils.Gen(point, new Shapes.Slime(20, xScale, 1f), Actions.Chain(
			new Modifiers.NotInShape(sideCarversShapeData),
			new Modifiers.Blotches(2, 0.4),
			new Actions.ClearTile(frameNeighbors: true).Output(slimeShapeData)
		));

		DecorateGrove(point, slimeShapeData);

		// Place the Butterfly Stump on the ground wherever applicable 
		bool placedStump = false;
		int placedStumpAttempts = 0;
		while (!placedStump)
		{
			placedStumpAttempts++;
			if (placedStumpAttempts > 5000)
				break;

			int randomX = WorldGen.genRand.Next(point.X - 8, point.X + 8);
			int randomY = WorldGen.genRand.Next(point.Y, point.Y + 12);
			WorldGen.PlaceTile(randomX, randomY, ModContent.TileType<ButterflyStump>(), mute: true, forced: false, -1);
			placedStump = Main.tile[randomX, randomY].TileType == ModContent.TileType<ButterflyStump>();
		}

		// If the former doesn't work, increase the range we search for a spot at
		if (placedStumpAttempts < 15000)
		{
			while (!placedStump)
			{
				placedStumpAttempts++;

				int randomX = WorldGen.genRand.Next(point.X - 16, point.X + 16);
				int randomY = WorldGen.genRand.Next(point.Y, point.Y + 14);
				WorldGen.PlaceTile(randomX, randomY, ModContent.TileType<ButterflyStump>(), mute: true, forced: false, -1);
				placedStump = Main.tile[randomX, randomY].TileType == ModContent.TileType<ButterflyStump>();
			}
		}
		// If everything fails, give up and log as an error
		else if (placedStumpAttempts >= 15000)
		{
			SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine Stump");
		}
	}

	public static void DecorateGrove(Point point, ShapeData slimeShapeData)
	{
		// Place grass along the inner outline of the cavern shape
		WorldUtils.Gen(point, new ModShapes.InnerOutline(slimeShapeData), Actions.Chain(
			new Actions.SetTile(TileID.Grass),
			new Actions.SetFrames(frameNeighbors: true)
		));

		// Place waterfalls around the upper half of the cavern
		int waterfallCap = WorldGen.genRand.Next(1, 3);
		int waterfallAmt = 0;
		WorldUtils.Gen(point, new ModShapes.InnerOutline(slimeShapeData), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Grass),
			new Modifiers.RectangleMask(-40, 40, -40, 0),
			new Actions.Custom((i, j, args) =>
			{
				if (WorldGen.genRand.NextBool(10))
				{
					if (waterfallAmt >= waterfallCap)
						return true;

					// Doing all our validation here, checking for two things...
					// 1. If the block to the left/right is air (so we know what direction to face the waterfall in)
					// 2. If there is no liquid where the water will be (to prevent duplicates)
					if (!Main.tile[i + 1, j].HasTile && Main.tile[i - 1, j].LiquidAmount == 0)
					{
						PlaceWaterfall(i, j, true);
						waterfallAmt++;
					}
					else if (!Main.tile[i - 1, j].HasTile && Main.tile[i + 1, j].LiquidAmount == 0)
					{
						PlaceWaterfall(i, j, false);
						waterfallAmt++;
					}
				}

				return true;
			})
		));

		// Place Flower wall on all cavern shape coordinates. Place flower vines 1 tile below all grass tiles of the cavern
		WorldUtils.Gen(point, new ModShapes.All(slimeShapeData), Actions.Chain(
			new Actions.PlaceWall(WallID.Flower),
			new Modifiers.RectangleMask(-40, 40, -40, -5),
			new Modifiers.OnlyTiles(TileID.Grass),
			new Modifiers.Offset(0, 1),
			new ActionVines(0, 12, 382)
		));

		// Place grass and flowers above grass tiles in the cavern
		WorldUtils.Gen(point, new ModShapes.All(slimeShapeData), Actions.Chain(
			new Modifiers.Offset(0, -1),
			new Modifiers.OnlyTiles(TileID.Grass),
			new Modifiers.Offset(0, -1),
			new ActionGrass()
		));

		// Place Sakura trees on the ground wherever applicable 
		WorldUtils.Gen(point, new ModShapes.All(slimeShapeData), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Grass),
			new Actions.Custom((i, j, args) => {
				if (WorldGen.genRand.NextBool())
					WorldGen.GrowTreeWithSettings(i, j, WorldGen.GrowTreeSettings.Profiles.VanityTree_Sakura);
				return true;
			})
		));
	}

	public static void PlaceWaterfall(int x, int y, bool leftIndent)
	{
		WorldGen.PoundTile(x, y);

		// Making an array with all the points we want to check for blocks before placing water
		// The X is always positive so we can left/rightshift it later based on waterfall direction
		Point[] tileCheckOffsets =
		[
			new(2, -1), // far top
            new(1, -1), // middle top
            new(0, -1), // near top
            new(2, 0),  // far middle
            new(2, 1),  // far bottom
            new(1, 1),  // middle bottom
            new(0, 1)   // near bottom
        ];

		// Iterate through our array and take care of any blocks that need taking care of
		Tile tile;
		for (int i = 0; i < tileCheckOffsets.Length; i++)
		{
			int horizOffset = leftIndent ? tileCheckOffsets[i].X * -1 : tileCheckOffsets[i].X;
			horizOffset += x;
			int vertOffset = tileCheckOffsets[i].Y + y;

			tile = Main.tile[horizOffset, vertOffset];
			if (!tile.HasTile)
			{
				tile.HasTile = true;
				tile.TileType = TileID.Grass;
				tile.WallType = WallID.Flower;
				WorldGen.SquareTileFrame(horizOffset, vertOffset);
			}
		}

		// Now we handle placing the water
		int waterHorizOffset = leftIndent ? -1 : 1;
		waterHorizOffset += x;
		tile = Main.tile[waterHorizOffset, y];

		if (tile.HasTile)
			tile.HasTile = false;

		tile.LiquidType = LiquidID.Water;
		tile.LiquidAmount = 255;
		tile.WallType = WallID.Flower;
	}
}
