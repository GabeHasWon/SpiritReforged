using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;
using SpiritReforged.Content.Forest.ButterflyStaff;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ButterflyMicropass : Micropass, IGenerationPage
{
	public class ButterflyShrineBiome : MicrobiomeSystem.Microbiome
	{
		/// <summary> A square approximation of how big one cavern usually is. </summary>
		public const int Size = 35;

		public Rectangle Area => new(Position.X - Size / 2, Position.Y - Size / 2, Size, Size);

		public override void WorldLoad(TagCompound tag)
		{
			base.WorldLoad(tag);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			const int tries = 20;
			int randomCount = WorldGen.genRand.Next(3, 6);

			for (int i = 0; i < randomCount; i++) //Place butterflies
			{
				var pos = Vector2.Zero;
				for (int t = 0; t < tries; t++)
				{
					pos = WorldGen.genRand.NextVector2FromRectangle(Area).ToWorldCoordinates();
					if (!Collision.SolidCollision(pos, 8, 8))
						break;
				}

				NPC.NewNPCDirect(new EntitySource_SpawnNPC(), pos, ModContent.NPCType<ButterflyCritter>()); //Withheld by PersistentNPCSystem
			}
		}
	}

	public override string WorldGenName => "Butterfly Shrines";

	[GenConfigurable(0, 50)]
	[Slider]
	internal static int ButterflyCountMax = 1;

	PageInfo IGenerationPage.Info => new()
	{
		CopiedPage = new CanyonEntrance(),
	};

	Mod IGenerationPage.Mod => SpiritReforgedMod.Instance;

	private static readonly ushort[] Ignore = [TileID.LivingWood, TileID.LeafBlock, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick, 
		(ushort)ModContent.TileType<SaltBlockDull>(), (ushort)ModContent.TileType<SaltBlockReflective>()];

	// Remnants will take care of our butterfly shrines on their end at some point, change in the future
	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxAttempts = 2000;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Butterfly");
		int count = 0;
		ButterflyCountMax = this.GetPage().ValueOrDefault(nameof(ButterflyCountMax), Main.maxTilesX / WorldGen.WorldSizeSmallX); // 1 shrine in small and medium worlds, 2 in large

		Point16 size = new(ButterflyShrineBiome.Size);
		int third = Main.maxTilesX / 3;

		for (int a = 0; a < maxAttempts; a++)
		{
			Point16 pt = new(
				WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(GenVars.leftBeachEnd, third) : WorldGen.genRand.Next(Main.maxTilesX - third, GenVars.rightBeachStart),
				(int)GenVars.worldSurface + WorldGen.genRand.Next(50, 100));

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(new Point(pt.X, pt.Y) - new Point(size.X / 2, size.Y / 2), new Shapes.Rectangle(size.X, size.Y), new Actions.TileScanner(TileID.Dirt).Output(typeToCount));

			if (typeToCount[TileID.Dirt] > size.X * size.Y * 0.5f && GenVars.structures.CanPlace(new Rectangle(pt.X, pt.Y, size.X, size.Y), 4))
			{
				var blacklist = new QuickConversion.BiomeType[] { QuickConversion.BiomeType.Jungle, QuickConversion.BiomeType.Mushroom, QuickConversion.BiomeType.Desert, QuickConversion.BiomeType.Ice };
				var biome = QuickConversion.FindConversionBiome(pt, size);

				if (blacklist.Contains(biome))
					continue;

				Rectangle area = new(pt.X + size.X / 2, pt.Y + size.Y / 2, size.X, size.Y);
				MicrobiomeSystem.Microbiome.Create<ButterflyShrineBiome>(area.Center);
				Generate(area);

				var origin = new Point(area.Center.X, area.Top + 8); //Top-centered position
				bool foundClearing = WorldUtils.Find(origin, Searches.Chain(new Searches.Up(1000), new Conditions.IsSolid().AreaOr(1, 50).Not()), out var top);
				top.Y += 50;

				if (foundClearing) //Generate a shaft like sword shrines do
				{
					ShapeData data = new();
					Point shaftOrigin = new(origin.X, top.Y + 10);
					int shaftHeight = origin.Y - top.Y - 9;

					//Sand wall fill
					WorldUtils.Gen(new(shaftOrigin.X - 1, shaftOrigin.Y - 1), new Shapes.Rectangle(3, shaftHeight + 2), Actions.Chain(
						new Modifiers.Blotches(2, 0.2),
						new Modifiers.OnlyTiles(TileID.Sand, TileID.HardenedSand, TileID.Sandstone),
						new Actions.PlaceWall(WallID.HardenedSand)
					));

					WorldUtils.Gen(shaftOrigin, new Shapes.Rectangle(1, shaftHeight), Actions.Chain(
						new Modifiers.Blotches(2, 0.2),
						new Modifiers.SkipTiles(Ignore),
						new Actions.ClearTile().Output(data),
						new Modifiers.Expand(1),
						new Modifiers.OnlyTiles(TileID.Sand),
						new Actions.SetTileKeepWall(TileID.HardenedSand).Output(data)
					));

					WorldUtils.Gen(new Point(origin.X, top.Y + 10), new ModShapes.All(data), new Actions.SetFrames(frameNeighbors: true));
				}

				if (++count >= ButterflyCountMax)
					return;
			}
		}

		SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine");
	}

	#region worldgen
	public static void Generate(Rectangle area)
	{
		ShapeData slimeShapeData = new();
		ShapeData sideCarversShapeData = new();
		Point location = area.Center;
		float xScale = 0.8f + WorldGen.genRand.NextFloat() * 0.25f; // Randomize the width of the shrine area

		// Create a masking layer for the cavern, so the walls tilt inwards while going up
		// The masking layer is comprised of two circles, offset left and right respectively
		int maskOffset = 30;
		WorldUtils.Gen(location, new Shapes.Circle(15), Actions.Chain(
			new Modifiers.Offset(maskOffset, -10),
			new Actions.Blank().Output(sideCarversShapeData)
		));

		WorldUtils.Gen(location, new Shapes.Circle(15), Actions.Chain(
			new Modifiers.Offset(-maskOffset, -10),
			new Actions.Blank().Output(sideCarversShapeData)
		));

		// Using the Slime shape, clear out tiles. Accomodate for the side carvers mask, to create a nice bell shape
		WorldUtils.Gen(location, new Shapes.Slime(20, xScale, 1f), Actions.Chain(
			new Modifiers.NotInShape(sideCarversShapeData),
			new Modifiers.Blotches(2, 0.4),
			new Actions.ClearTile(frameNeighbors: true).Output(slimeShapeData)
		));

		DecorateGrove(location, slimeShapeData);

		// Place the Butterfly Stump on the ground wherever applicable 
		bool placedStump = false;
		int placedStumpAttempts = 0;
		while (!placedStump)
		{
			placedStumpAttempts++;
			if (placedStumpAttempts > 5000)
				break;

			int randomX = WorldGen.genRand.Next(location.X - 8, location.X + 8);
			int randomY = WorldGen.genRand.Next(location.Y, location.Y + 12);
			WorldGen.PlaceTile(randomX, randomY, ModContent.TileType<ButterflyStump>(), mute: true, forced: false, -1);
			placedStump = Main.tile[randomX, randomY].TileType == ModContent.TileType<ButterflyStump>();
		}

		// If the former doesn't work, increase the range we search for a spot at
		if (placedStumpAttempts < 15000)
			while (!placedStump)
			{
				placedStumpAttempts++;

				int randomX = WorldGen.genRand.Next(location.X - 16, location.X + 16);
				int randomY = WorldGen.genRand.Next(location.Y, location.Y + 14);
				WorldGen.PlaceTile(randomX, randomY, ModContent.TileType<ButterflyStump>(), mute: true, forced: false, -1);
				placedStump = Main.tile[randomX, randomY].TileType == ModContent.TileType<ButterflyStump>();
			}
		else if (placedStumpAttempts >= 15000) // If everything fails, give up and log as an error
			SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine Stump");

		GenVars.structures.AddProtectedStructure(area, 4);
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
	#endregion
}