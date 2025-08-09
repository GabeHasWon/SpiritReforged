using SpiritReforged.Content.Forest.ButterflyStaff;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;

public class ButterflyShrineBiome : Microbiome
{
	public static readonly Point16 Size = new(35); //An approximation of how big one cavern usually is
	public Rectangle Rectangle => new(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);

	public override void WorldLoad(TagCompound tag)
	{
		base.WorldLoad(tag);

		if (Main.netMode != NetmodeID.MultiplayerClient)
			Populate(new Rectangle(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y));
	}

	/// <summary> Spawns butterfly NPCs within <paramref name="rect"/>. </summary>
	private static void Populate(Rectangle rect)
	{
		const int tries = 20;
		int randomCount = Main.rand.Next(3, 6);

		for (int i = 0; i < randomCount; i++)
		{
			var pos = Vector2.Zero;
			for (int t = 0; t < tries; t++)
			{
				pos = Main.rand.NextVector2FromRectangle(rect).ToWorldCoordinates();
				if (!Collision.SolidCollision(pos, 8, 8))
					break;
			}

			NPC.NewNPCDirect(new EntitySource_SpawnNPC(), pos, ModContent.NPCType<ButterflyCritter>()); //Withheld by PersistentNPCSystem
		}
	}

	#region worldgen
	protected override void OnPlace(Point16 origin)
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
			while (!placedStump)
			{
				placedStumpAttempts++;

				int randomX = WorldGen.genRand.Next(point.X - 16, point.X + 16);
				int randomY = WorldGen.genRand.Next(point.Y, point.Y + 14);
				WorldGen.PlaceTile(randomX, randomY, ModContent.TileType<ButterflyStump>(), mute: true, forced: false, -1);
				placedStump = Main.tile[randomX, randomY].TileType == ModContent.TileType<ButterflyStump>();
			}
		else if (placedStumpAttempts >= 15000) // If everything fails, give up and log as an error
			SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine Stump");

		GenVars.structures.AddProtectedStructure(new Rectangle(origin.X, origin.Y, Size.X, Size.Y), 4);
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