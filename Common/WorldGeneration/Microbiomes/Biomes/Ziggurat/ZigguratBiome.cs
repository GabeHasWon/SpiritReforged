using Microsoft.CodeAnalysis;
using ReLogic.Utilities;
using SpiritReforged.Content.Desert.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public class ZigguratBiome : Microbiome
{
	/// <summary> The maximum width of the biome. </summary>
	public const int Width = 180;
	/// <summary> The maximum height of the biome. </summary>
	public const int Height = 90;

	public const int HallwayWidth = 4;

	private static HashSet<Rectangle> TotalBounds;
	private static HashSet<GenRoom> TotalRooms;

	protected override void OnPlace(Point16 point)
	{
		Rectangle area = new(point.X - Width / 2, point.Y - Height / 2, Width, Height);

		CreateShape(area, 4, 0.25f, 1.3f, out var bounds);
		TotalBounds = [.. bounds];

		AddRooms(bounds, out var rooms);
		TotalRooms = [.. rooms];

		Sandify(bounds, 2);
		CreateHallways(rooms);
	}

	/// <summary> </summary>
	/// <param name="area"> The total area the structure can occupy. </param>
	/// <param name="layers"> The number of distinct layers in the structure. </param>
	/// <param name="minWidth"> A gradual width multiplier for the top layer. </param>
	/// <param name="finalHeight"> A height multiplier for the bottom layer. </param>
	/// <param name="bounds"></param>
	private static void CreateShape(Rectangle area, int layers, float minWidth, float finalHeight, out List<Rectangle> bounds)
	{
		int finalLayerHeight = (int)(area.Height / layers * finalHeight);
		int commonLayerHeight = (area.Height - finalLayerHeight) / (layers - 1);
		bounds = [];

		Point center = new(area.Center.X, area.Y + commonLayerHeight / 2);

		for (int y = 0; y < layers; y++)
		{
			bool finalLayer = y == layers - 1;

			int width = (int)MathHelper.Lerp(area.Width * minWidth, area.Width, y / (layers - 1f));
			int height = finalLayer ? finalLayerHeight : commonLayerHeight;
			var topLeft = center - new Point(width / 2, commonLayerHeight / 2);

			WorldUtils.Gen(topLeft, new Shapes.Rectangle(width, height), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile((ushort)ModContent.TileType<RedSandstoneBrick>())
			));

			WorldUtils.Gen(topLeft + new Point(1, 1), new Shapes.Rectangle(width - 2, height), new Actions.PlaceWall(WallID.SandstoneEcho));
			Rectangle bound = new(topLeft.X, topLeft.Y, width, height);

			bounds.Add(bound);
			center.Y += height;

			CreateHalo(bound);
		}

		//Add weathering around some edges of the shape
		ShapeData shape = new();
		ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

		WorldUtils.Gen(area.Location, new Shapes.Rectangle(area.Width, area.Height), Actions.Chain(
			new Modifiers.OnlyTiles(brick),
			new Actions.Blank()
		).Output(shape));

		WorldUtils.Gen(area.Location, new ModShapes.InnerOutline(shape), Actions.Chain(
			new Modifiers.Dither(),
			new Modifiers.Blotches(2, 0.1),
			new Modifiers.IsTouching(false, TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sand)
		));

		static void CreateHalo(Rectangle bounds)
		{
			const int width = 5;
			ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

			WorldUtils.Gen(new(bounds.X, bounds.Y - 2), new Shapes.Rectangle(width, 2), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile(brick)
			));

			WorldUtils.Gen(new(bounds.X + bounds.Width - width, bounds.Y - 2), new Shapes.Rectangle(width, 2), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile(brick)
			));
		}
	}

	/// <summary> Randomly adds large sandy spots starting from a select item in <paramref name="bounds"/>. </summary>
	private static void Sandify(List<Rectangle> bounds, int count)
	{
		const int scanRadius = 3;
		int successes = 0;

		for (int a = 0; a < 20; a++)
		{
			var b = bounds[WorldGen.genRand.Next(2, bounds.Count)];
			var origin = new Vector2(WorldGen.genRand.Next([b.Left, b.Right]), WorldGen.genRand.Next([b.Top, b.Bottom])).ToPoint();

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(new(origin.X - scanRadius, origin.Y - scanRadius), new Shapes.Circle(scanRadius), new Actions.TileScanner(TileID.Sand).Output(typeToCount));

			if (typeToCount[TileID.Sand] < scanRadius * scanRadius * 0.25f)
				continue; //Check if origin is close to sand

			int spotCount = WorldGen.genRand.Next(4, 8);
			ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

			WorldUtils.Gen(origin, new GenTypes.Splatter(15, WorldGen.genRand.Next(4, 9), 34), Actions.Chain(
				new Modifiers.Blotches(),
				new Modifiers.OnlyTiles(brick),
				new Actions.Custom(static (i, j, args) =>
				{
					ushort type = WorldGen.TileIsExposedToAir(i, j) ? TileID.Sandstone : TileID.Sand;
					Main.tile[i, j].ResetToType(type);

					if (type == TileID.Sandstone && WorldGen.genRand.NextBool(12))
						WorldUtils.Gen(new(i, j), new Shapes.Tail(WorldGen.genRand.Next(3, 6), new Vector2D(0, WorldGen.genRand.Next(4, 16))), new Actions.SetTileKeepWall(TileID.Sandstone));

					return true;
				}),
				new Actions.PlaceWall(WallID.HardenedSand)
			));

			if (++successes >= count)
				break;
		}
	}

	private static void AddRooms(IEnumerable<Rectangle> bounds, out List<GenRoom> rooms)
	{
		const int paddedWidth = HallwayWidth + 10;
		const int paddedHeight = 2;

		rooms = [];

		Rectangle[] orderedBounds = [.. bounds.OrderBy(static x => x.Location.Y)];
		for (int i = 0; i < orderedBounds.Length; i++)
		{
			var bound = orderedBounds[i];
			Rectangle innerBounds = new(bound.X + paddedWidth, bound.Y + paddedHeight, bound.Width - paddedWidth * 2, bound.Height - paddedHeight * 2);

			if (i == 0)
			{
				var r = new ZigguratRooms.EntranceRoom(bound);
				r.SetOrigin(new(bound.Center.X, bound.Bottom - r.Bounds.Height / 2 - 2)).Create();

				rooms.Add(r);
			}
			else
			{
				int skip = WorldGen.genRand.Next(i + 1);
				int totalCount = i + 1;

				for (int x = 0; x <= totalCount; x++)
				{
					float progress = (float)x / totalCount;
					var r = new ZigguratRooms.BasicRoom(bound);

					if (i == orderedBounds.Length - 1) //Final layer
					{
						if (x == 0)
						{
							r = new ZigguratRooms.TreasureRoom(bound);
							progress = WorldGen.genRand.NextFloat();
						}
						else if (WorldGen.genRand.NextBool())
						{
							r = new ZigguratRooms.SandyRoom(bound);
						}
					}
					else if (x == skip)
					{
						continue;
					}

					Point origin = new((int)MathHelper.Lerp(innerBounds.Left + r.Bounds.Width / 2, innerBounds.Right - r.Bounds.Width / 2, progress), innerBounds.Bottom - r.Bounds.Height / 2);
					if (Framing.GetTileSafely(origin).TileType == TileID.Sand)
					{
						r = new ZigguratRooms.SandyRoom(bound);
					}
					
					r.SetOrigin(origin);

					if (!rooms.Any(x => x.Intersects(r.Bounds, 2))) //If we don't care about rooms occasionally intersecting, remove this check
					{
						r.Create();
						rooms.Add(r);
					}
				}
			}
		}
	}

	private static void CreateHallways(IEnumerable<GenRoom> rooms)
	{
		foreach (var start in rooms.OrderBy(static x => x.Origin.Y))
		{
			foreach (var end in rooms.OrderBy(static x => x.Origin.Y))
			{
				if (start != end && TryLink(start, end))
					break;
			}
		}

		foreach (var r in rooms)
		{
			if (r is ZigguratRooms.BasicRoom b)
				b.PostPlaceHallways();
		}

		static bool TryLink(GenRoom a, GenRoom b)
		{
			foreach (var start in a.Links)
			{
				foreach (var end in b.Links)
				{
					if (AddHallway(start, end))
					{
						a.Links.Remove(start);

						if (WorldGen.genRand.NextBool(4))
							b.Links.Remove(end); //Allow the end link to branch out an additional time with a 75% chance

						return true;
					}
				}
			}

			return false;
		}
	}

	private static bool AddHallway(GenRoom.Link startLink, GenRoom.Link endLink)
	{
		var start = startLink.Location;
		var end = endLink.Location;

		for (int a = 0; a < 2; a++)
		{
			//A safe distance from the starting link
			var entrance = new Point(start.X + startLink.Direction.X * HallwayWidth, start.Y + startLink.Direction.Y * HallwayWidth);
			//A safe distance from the ending link
			var exit = new Point(end.X + endLink.Direction.X * HallwayWidth, end.Y + endLink.Direction.Y * HallwayWidth);

			bool sloped = a == 0;
			var down = sloped ? new Point(entrance.X + (end.Y - start.Y) * startLink.Direction.X, end.Y) : new Point(entrance.X, end.Y); //Straight or diagonal down given enough space

			if (!Intersecting(entrance, down, exit) && Contains(entrance) && Contains(down))
			{
				BlockOut(start, entrance, HallwayWidth);
				BlockOut(entrance, down, HallwayWidth);

				BlockOut(down, exit, HallwayWidth);
				BlockOut(exit, end, HallwayWidth);

				if (!sloped) //Avoid placing platforms at sloped entrances
				{
					var shelf = (entrance.Y < down.Y) ? entrance : down;
					WorldUtils.Gen(new(shelf.X - 2, shelf.Y + 3), new Shapes.Rectangle(HallwayWidth, 1), new Actions.PlaceTile(TileID.Platforms, 42));
				}

				return true;
			}
		}

		return false;

		static bool Intersecting(params Point[] points)
		{
			const int padding = 2;
			float collisionPoint = 0;

			for (int i = 0; i < points.Length - 1; i++)
			{
				var startVector = points[i].ToVector2();
				var endVector = points[i + 1].ToVector2();

				foreach (var r in TotalRooms)
				{
					if (Collision.CheckAABBvLineCollision(r.Bounds.TopLeft() - new Vector2(padding), r.Bounds.Size() + new Vector2(padding * 2), startVector, endVector, HallwayWidth, ref collisionPoint))
						return true;
				}
			}

			return false;
		}
	}

	/// <summary> Clears and smooths tiles between <paramref name="start"/> and <paramref name="end"/> based on <paramref name="width"/>. </summary>
	public static void BlockOut(Point start, Point end, int width)
	{
		Vector2 startCoords = new(start.X, start.Y);
		Vector2 endCoords = new(end.X, end.Y);

		int length = (int)Math.Round(startCoords.Distance(endCoords));

		if (length <= 1)
			return;

		for (int i = 0; i <= length; i++)
		{
			var intermediate = Vector2.Lerp(startCoords, endCoords, (float)i / length);
			Point origin = new((int)(intermediate.X - width / 2), (int)(intermediate.Y - width / 2));

			ShapeData shape = new();

			WorldUtils.Gen(origin, new Shapes.Rectangle(width, width), new Actions.ClearTile(true).Output(shape));
			WorldUtils.Gen(origin, new ModShapes.OuterOutline(shape), new Actions.Smooth());
		}
	}

	/// <summary> Checks whether <paramref name="point"/> is contained within the structure. </summary>
	public static bool Contains(Point point)
	{
		foreach (var bound in TotalBounds)
		{
			if (bound.Contains(point))
				return true;
		}

		return false;
	}
}