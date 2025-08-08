using ReLogic.Utilities;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class UndergroundOasisMicropass : Micropass
{
	public override string WorldGenName => "Underground Oasis";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Full Desert"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxAttempts = 200;
		const int area = 20;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int attempts = 0;
		int amount = 3 * (WorldGen.GetWorldSize() + 1);
		Rectangle region = new(GenVars.desertHiveLeft, (int)Main.worldSurface + 40, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - GenVars.desertHiveHigh);

		for (int i = 0; i < amount; i++)
		{
			var pt = Main.rand.NextVector2FromRectangle(region).ToPoint();

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(pt - new Point(area / 2, area / 2), new Shapes.Rectangle(area, area), new Actions.TileScanner(TileID.Sand, TileID.Sandstone, TileID.HardenedSand).Output(typeToCount));

			if (typeToCount[TileID.Sand] + typeToCount[TileID.Sandstone] + typeToCount[TileID.HardenedSand] < area * area * 0.7f)
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			PlaceOasis(pt);
		}
	}

	public static void PlaceOasis(Point origin)
	{
		Point size = new(WorldGen.genRand.Next(25, 40), WorldGen.genRand.Next(30, 40));
		ShapeData shape = new();

		//Base material
		WorldUtils.Gen(new Point(origin.X, origin.Y), new Shapes.Circle(size.X, 10), Actions.Chain(
			new Modifiers.Blotches(2, 0.4),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		).Output(shape));

		WorldUtils.Gen(new Point(origin.X, origin.Y - 2), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sandstone),
			new Actions.SetTileKeepWall(TileID.Sand)
		));

		//Clearing shape
		WorldUtils.Gen(origin, new Shapes.Mound(size.X, size.Y), Actions.Chain(
			new Modifiers.Blotches(2, 0.4),
			new Actions.ClearTile(frameNeighbors: true)
		).Output(shape));

		WorldUtils.Gen(new Point(origin.X, origin.Y - 12), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		));

		//Clearing walls
		WorldUtils.Gen(new Point(origin.X, origin.Y + 2), new Shapes.HalfCircle(size.X / 2), Actions.Chain(
			new Modifiers.IsNotSolid(),
			new Modifiers.Blotches(3),
			new Actions.ClearWall()
		));

		int lakeCount = WorldGen.genRand.NextBool(4) ? 2 : 1;
		int deviation = size.X / 2;

		for (int i = 0; i < lakeCount; i++)
		{
			Point lakeOrigin = new(origin.X + Main.rand.Next(-deviation, deviation), origin.Y);
			CarveLake(lakeOrigin);
		}

		PlaceStalactites(new Rectangle(origin.X - size.X / 2, origin.Y - 5, size.X, 10), WorldGen.genRand.Next(4, 8));
		Decorate(origin, shape);
	}

	private static void Decorate(Point origin, ShapeData clearingShape)
	{
		WorldUtils.Gen(origin, new ModShapes.All(clearingShape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.Custom((i, j, args) => {
				j--;
				if (Main.tile[i, j].LiquidAmount > 100 || Main.tile[i, j].HasTile || Main.tile[i, j - 1].HasTile)
					return false;

				if (WorldGen.genRand.NextBool(7))
				{
					int type = ModContent.TileType<Glowflower>();
					Placer.PlaceTile(i, j, type);
				}

				return true;
			})
		));

		WorldUtils.Gen(origin, new ModShapes.All(clearingShape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.Custom((i, j, args) => {
				if (WorldGen.genRand.NextBool(3))
					WorldGen.GrowPalmTree(i, j);
				return true;
			})
		));
	}

	private static void PlaceStalactites(Rectangle area, int count)
	{
		HashSet<Point> points = [];

		for (int i = 0; i < count; i++)
		{
			var point = Main.rand.NextVector2FromRectangle(area).ToPoint();
			int x = point.X;
			int y = point.Y;

			while (WorldGen.InWorld(x, y, 2) && !WorldGen.SolidTile(x, y))
				y--;

			if (Main.tile[x, y].TileType != TileID.Sandstone)
				continue;

			points.Add(new(x, y));
		}

		foreach (var pt in points)
		{
			WorldUtils.Gen(pt, new Shapes.Tail(WorldGen.genRand.Next(3, 6), new Vector2D(0, WorldGen.genRand.Next(8, 16))), Actions.Chain(
				new Actions.SetTileKeepWall(TileID.Sandstone)
			));
		}
	}

	private static void CarveLake(Point origin)
	{
		WorldMethods.FindGround(origin.X, ref origin.Y);

		WorldUtils.Gen(origin, new Shapes.Circle(8, 4), Actions.Chain(
			new Modifiers.IsSolid(),
			new Actions.ClearTile(),
			new Actions.SetLiquid(LiquidID.Water)
		));

		//Clear surrounding walls
		WorldUtils.Gen(new Point(origin.X, origin.Y + 4), new Shapes.HalfCircle(5), Actions.Chain(
			new Modifiers.IsNotSolid(),
			new Modifiers.Blotches(),
			new Actions.ClearWall()
		));

		Vector2 size = new(50);
		WorldDetours.Regions.Add(new(new Rectangle(origin.X - (int)(size.X / 2), origin.Y - (int)(size.Y / 2), (int)size.X, (int)size.Y), WorldDetours.Context.Lava));
	}
}