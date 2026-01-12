using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;
using System.Linq;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class OasisMicropass : Micropass
{
	public override string WorldGenName => "Underground Oasis";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxAttempts = 200;
		const int area = 50;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int attempts = 0;
		int amount = 3 * (WorldGen.GetWorldSize() + 1);
		Rectangle region = new(GenVars.desertHiveLeft, (int)Main.worldSurface + 40, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - GenVars.desertHiveHigh);

		HashSet<Rectangle> biomesRectangles = [];

		for (int i = 0; i < amount; i++)
		{
			var pt = WorldGen.genRand.NextVector2FromRectangle(region).ToPoint();

			if (!GenVars.structures.CanPlace(new Rectangle(pt.X - area / 2, pt.Y - area / 2, area, area), 4) || biomesRectangles.Any(x => x.Contains(pt)))
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(pt - new Point(area / 2, area / 2), new Shapes.Rectangle(area, area), new Actions.TileScanner(TileID.Sand, TileID.Sandstone, TileID.HardenedSand).Output(typeToCount));

			if (typeToCount[TileID.Sand] + typeToCount[TileID.Sandstone] + typeToCount[TileID.HardenedSand] < area * area * 0.5f)
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			var biome = Microbiome.Create<UndergroundOasisBiome>(pt);
			Rectangle rectangle = biome.Rectangle;
			rectangle.Inflate(100, 100);

			biomesRectangles.Add(rectangle);
			int ruinCount = WorldGen.genRand.Next(3);

			if (ruinCount > 0)
			{
				Rectangle ruinsArea = biome.Rectangle with { Y = biome.Rectangle.Y - 20 };
				ruinsArea.Inflate(10, 10);

				WorldMethods.Generate(GenerateRuins, ruinCount, out _, ruinsArea, 50);
			}
		}
	}

	//Generate oasis ruins, adapted from ZigguratMicropass
	#region ruins
	private static bool GenerateRuins(int x, int y)
	{
		const int suspension = 8; //Force the structure to be suspended a minimum number of tiles
		if (WorldGen.SolidOrSlopedTile(x, y) || !WorldUtils.Find(new(x, y), new Searches.Down(30).Conditions(new Conditions.IsSolid()), out Point foundPos) || foundPos.Y - y < suspension)
			return false;

		Rectangle region = CreateRuin(foundPos.X, foundPos.Y - 10, WorldGen.genRand.Next(2, 5));

		GenVars.structures.AddProtectedStructure(region);
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Walls));
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Piles));

		return true;
	}

	/// <summary> Generates a desert ruin at the provided location with <paramref name="segments"/> that disrupts tiles. </summary>
	/// <param name="x"> The X coordinate. </param>
	/// <param name="y"> The Y Coordinate. </param>
	/// <param name="segments"> The number of room segments to queue. </param>
	/// <returns> The total area occupied by the ruin. </returns>
	public static Rectangle CreateRuin(int x, int y, int segments)
	{
		ZigguratMicropass.CreateArray(new(x - 4, y - 4, 8, 8), GetUpwardDirections(segments), out List<Rectangle> areas);
		Rectangle result = ZigguratMicropass.Maximize(areas);

		segments = areas.Count; //Reassign segments to be consistent with our number of predetermined areas
		var shapeData = Enumerable.Repeat(new ShapeData(), segments).ToArray();

		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Location, new Shapes.Rectangle(a.Width, a.Height), Actions.Chain(
				new Actions.ClearTile(), 
				new Actions.PlaceWall((ushort)ModContent.WallType<PolishedSandstoneWall>()), 
				new Modifiers.RectangleMask(2, a.Width - 2 - 1, 0, a.Height), 
				new Actions.PlaceWall((ushort)ModContent.WallType<RedSandstoneBrickWall>())
			).Output(shapeData[c]));

			ShapeData windowData = new();
			WorldUtils.Gen(a.Location, new ModShapes.All(shapeData[c]), Actions.Chain(
				new Modifiers.RectangleMask(3, a.Width - 3 - 1, 0, a.Height - 3),
				new Actions.PlaceWall((ushort)ModContent.WallType<BronzeGrate>())
			).Output(windowData)); //Add windows

			WorldUtils.Gen(a.Location, new ModShapes.All(windowData), Actions.Chain(
				new Modifiers.Dither(WorldGen.genRand.NextFloat(0.9f)),
				new Actions.ClearWall()
			).Output(windowData)); //Add window dithering

			for (int p = 0; p < 2; p++)
			{
				Point pillarPosition = a.Location + new Point((a.Width - 1) * p, 0);
				WorldUtils.Gen(pillarPosition, new Shapes.Rectangle(1, a.Height), Actions.Chain(
					new Actions.PlaceTile((ushort)ModContent.TileType<RuinedSandstonePillar>())
				));
			}
		} //Generate all segment walls first and collect ShapeData

		ushort[] skipWallTypes = [(ushort)ModContent.WallType<RedSandstoneBrickWall>(), (ushort)ModContent.WallType<PolishedSandstoneWall>()];
		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Location, new ModShapes.OuterOutline(shapeData[c]), Actions.Chain(
				new Modifiers.SkipWalls(skipWallTypes), 
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

		result.Inflate(2, 2);
		new Decorator(result)
			.Enqueue(ModContent.TileType<AncientBanner>(), WorldGen.genRand.Next(1, 4))
			.Run();

		return result;

		static Point[] GetUpwardDirections(int length)
		{
			var result = new Point[length];

			for (int c = 0; c < length; c++)
				result[c] = WorldGen.genRand.NextBool() ? new(WorldGen.genRand.NextFromList(-1, 1), 0) : new(0, -1);

			return result;
		}
	}

	private static void DropPillar(int x, int y, int tileType, int wallType, out int lowestY, int length = 0)
	{
		lowestY = y;
		int currentLength = 0;

		while ((length == 0 || currentLength < length) && WorldGen.InWorld(x, y, 20) && !WorldGen.SolidOrSlopedTile(x, y) && Main.tile[x, y] is Tile tile && !Main.wallHouse[tile.WallType])
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
	#endregion
}