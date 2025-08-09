using ReLogic.Utilities;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisBiome : Microbiome
{
	//Preface with basic relevant checks so linq isn't constantly running in the background
	public static bool InUndergroundOasis(Player p) => p.Center.Y / 16 > Main.worldSurface && p.ZoneDesert && MicrobiomeSystem.Microbiomes.Any(x => x is UndergroundOasisBiome o && o.Rectangle.Contains(p.Center.ToTileCoordinates()));

	public static readonly Point16 Size = new(50, 40);
	public Rectangle Rectangle => new(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);

	public override void Load() => NPCEvents.OnEditSpawnRate += ReduceSpawns;
	private static void ReduceSpawns(Player player, ref int spawnRate, ref int maxSpawns)
	{
		if (InUndergroundOasis(player))
		{
			spawnRate *= 5;
			maxSpawns = 0;
		}
	}

	#region worldgen
	protected override void OnPlace(Point16 point)
	{
		var origin = point.ToPoint();
		Point radius = new(WorldGen.genRand.Next(30, 35), WorldGen.genRand.Next(45, 70));
		ShapeData shape = new();

		//Base material
		WorldUtils.Gen(new Point(origin.X, origin.Y), new Shapes.Circle(radius.X, 10), Actions.Chain(
			new Modifiers.Blotches(2, 0.4),
			new Modifiers.SkipTiles(TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		).Output(shape));

		WorldUtils.Gen(new Point(origin.X, origin.Y - 2), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sandstone),
			new Actions.SetTileKeepWall(TileID.Sand)
		));

		//Clearing shape
		WorldUtils.Gen(origin, new Shapes.Mound(radius.X, radius.Y / 2), Actions.Chain(
			new Modifiers.RectangleMask(-(radius.X - 5), radius.X - 5, -radius.Y, radius.Y),
			new Modifiers.Blotches(),
			new Actions.ClearTile(frameNeighbors: true)
		).Output(shape));

		WorldUtils.Gen(origin, new ModShapes.All(shape), new Actions.Smooth());

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

		PlaceStalactites(origin, radius.X, WorldGen.genRand.Next(4, 8));
		Decorate(origin, shape);
		PlaceLightShaft(origin);

		GenVars.structures.AddProtectedStructure(new Rectangle(origin.X - Size.X / 2, origin.Y - Size.Y / 2, Size.X, Size.Y), 4);
	}

	private static void Decorate(Point origin, ShapeData clearingShape)
	{
		WorldUtils.Gen(origin, new ModShapes.All(clearingShape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.Custom((i, j, args) => {
				j--;
				if (Main.tile[i, j].HasTile || Main.tile[i, j - 1].HasTile)
					return false;

				if (Main.tile[i, j].LiquidAmount > 100)
				{
					if (WorldGen.genRand.NextBool(3))
					{
						WorldGen.PlaceCatTail(i, j);
					}
				}
				else
				{
					if (WorldGen.genRand.NextBool(3))
					{
						WorldGen.PlaceOasisPlant(i, j);
					}

					if (WorldGen.genRand.NextBool(5))
					{
						Placer.PlaceTile(i, j, ModContent.TileType<Glowflower>());
					}
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

	private static void PlaceStalactites(Point origin, int radius, int count)
	{
		int maxAttempts = 10 * count;
		int attempts = 0;
		HashSet<Point> points = [];

		for (int i = 0; i < count; i++)
		{
			var point = new Point(origin.X + Main.rand.Next(4, radius) * Main.rand.Next([-1, 1]), origin.Y);
			int x = point.X;
			int y = point.Y;

			while (WorldGen.InWorld(x, y, 2) && !WorldGen.SolidTile(x, y))
				y--;

			if (Main.tile[x, y].TileType != TileID.Sandstone)
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			points.Add(new(x, y));
		}

		ShapeData shape = new();
		foreach (var pt in points)
		{
			WorldUtils.Gen(pt, new Shapes.Tail(WorldGen.genRand.Next(3, 6), new Vector2D(0, WorldGen.genRand.Next(4, 16))), Actions.Chain(
				new Actions.SetTileKeepWall(TileID.Sandstone),
				new Modifiers.Expand(1),
				new Actions.PlaceWall(WallID.Sandstone)
			).Output(shape));
		}
	}

	private static void CarveLake(Point origin)
	{
		WorldMethods.FindGround(origin.X, ref origin.Y);
		ShapeData shape = new();

		WorldUtils.Gen(origin, new Shapes.Circle(WorldGen.genRand.Next(6, 11), WorldGen.genRand.Next(3, 6)), Actions.Chain(
			new Modifiers.IsSolid(),
			new Actions.ClearTile(),
			new Actions.SetLiquid(LiquidID.Water)
		).Output(shape));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(shape), new Actions.Smooth());

		Vector2 size = new(50);
		WorldDetours.Regions.Add(new(new Rectangle(origin.X - (int)(size.X / 2), origin.Y - (int)(size.Y / 2), (int)size.X, (int)size.Y), WorldDetours.Context.Lava));
	}
	#endregion
}