using ReLogic.Utilities;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisBiome : Microbiome
{
	public static bool InUndergroundOasis(Player p) => MicrobiomeSystem.Microbiomes.Any(x => x is UndergroundOasisBiome o && o.Rectangle.Contains(p.Center.ToTileCoordinates()));

	public static readonly Point16 Size = new(80, 50);
	public Rectangle Rectangle => new(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);

	#region worldgen
	protected override void OnPlace(Point16 point)
	{
		var origin = point.ToPoint();
		Point radius = new(WorldGen.genRand.Next(30, 40), WorldGen.genRand.Next(25, 35));
		ShapeData shape = new();

		//Base material
		WorldUtils.Gen(new Point(origin.X, origin.Y), new Shapes.Circle(radius.X, 10), Actions.Chain(
			new Modifiers.Blotches(2, 0.4),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		).Output(shape));

		WorldUtils.Gen(new Point(origin.X, origin.Y - 2), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sandstone),
			new Actions.SetTileKeepWall(TileID.Sand)
		));

		//Clearing shape
		WorldUtils.Gen(origin, new Shapes.Mound(radius.X, radius.Y), Actions.Chain(
			new Actions.ClearTile(frameNeighbors: true)
		).Output(shape));

		WorldUtils.Gen(new Point(origin.X, origin.Y - 12), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		));

		//Clearing walls
		WorldUtils.Gen(new Point(origin.X, origin.Y + 2), new Shapes.HalfCircle((int)(radius.X * 0.75f)), Actions.Chain(
			new Modifiers.IsNotSolid(),
			new Modifiers.Blotches(3),
			new Actions.ClearWall()
		));

		int deviation = radius.X / 2;
		Point lakeOrigin = new(origin.X + Main.rand.Next(-deviation, deviation), origin.Y);
		CarveLake(lakeOrigin);

		PlaceStalactites(new Rectangle(origin.X - radius.X / 2, origin.Y - 5, radius.X, 10), WorldGen.genRand.Next(4, 8));
		Decorate(origin, shape);
		PlaceLightShaft(origin);
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

	private static void PlaceLightShaft(Point point)
	{
		int x = point.X;
		int y = point.Y;

		while (WorldGen.InWorld(x, y, 2) && !WorldGen.SolidTile(x, y))
			y--;

		SimpleEntitySystem.NewEntity<LightShaft>(new Vector2(x, y).ToWorldCoordinates(), true);
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
			WorldUtils.Gen(pt, new Shapes.Tail(WorldGen.genRand.Next(3, 6), new Vector2D(0, WorldGen.genRand.Next(4, 16))), Actions.Chain(
				new Actions.SetTileKeepWall(TileID.Sandstone),
				new Modifiers.Expand(1),
				new Actions.PlaceWall(WallID.Sandstone)
			));
		}
	}

	private static void CarveLake(Point origin)
	{
		WorldMethods.FindGround(origin.X, ref origin.Y);

		WorldUtils.Gen(origin, new Shapes.Circle(WorldGen.genRand.Next(5, 11), WorldGen.genRand.Next(3, 6)), Actions.Chain(
			new Modifiers.IsSolid(),
			new Actions.ClearTile(),
			new Actions.SetLiquid(LiquidID.Water)
		));

		Vector2 size = new(50);
		WorldDetours.Regions.Add(new(new Rectangle(origin.X - (int)(size.X / 2), origin.Y - (int)(size.Y / 2), (int)size.X, (int)size.Y), WorldDetours.Context.Lava));
	}
	#endregion
}