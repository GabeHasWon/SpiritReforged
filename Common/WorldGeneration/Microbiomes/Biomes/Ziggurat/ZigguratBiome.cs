using Microsoft.CodeAnalysis;
using ReLogic.Utilities;
using SpiritReforged.Content.Desert;
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

		CreateHallways(rooms);

		foreach (var r in rooms)
			r.Create();

		Sandify(bounds);
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

			WorldUtils.Gen(topLeft + new Point(1, 1), new Shapes.Rectangle(width - 2, height), new Actions.PlaceWall(WallID.Sandstone));
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

	/// <summary> Randomly adds weathering. </summary>
	private static void Sandify(List<Rectangle> bounds)
	{
		foreach (Rectangle b in bounds)
		{
			WorldMethods.GenerateSquared(static (i, j) =>
			{
				var tile = Main.tile[i, j];
				if (WorldGen.genRand.NextBool(50) && tile.HasTile && tile.TileType == ModContent.TileType<RedSandstoneBrick>() && !WorldGen.SolidTile3(i, j - 1))
				{
					ShapeData shape = new();
					int size = WorldGen.genRand.Next(2, 5);

					WorldUtils.Gen(new(i, j), new Shapes.Mound(size, size), Actions.Chain(
						new Modifiers.Flip(false, true),
						new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
						new Actions.Custom((x, y, args) =>
						{
							if (y == j)
							{
								Main.tile[x, y].ClearTile();
							}
							else
							{
								ushort type = WorldGen.SolidTile3(x, y + 1) ? TileID.Sand : TileID.HardenedSand;
								Main.tile[x, y].ResetToType(type);
							}

							return false;
						})
					).Output(shape));

					WorldUtils.Gen(new(i, j), new ModShapes.OuterOutline(shape), Actions.Chain(
						new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
						new Modifiers.Dither(),
						new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrickCracked>())
					));
				}

				if (WorldGen.genRand.NextBool(80) && tile.HasTile && tile.TileType == TileID.Sand)
				{
					WorldUtils.Gen(new(i, j), new GenTypes.Splatter(8, 10, 20), Actions.Chain(
						new Modifiers.Blotches(),
						new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>(), (ushort)ModContent.TileType<RedSandstoneBrickCracked>()),
						new Actions.Custom(static (x, y, args) =>
						{
							ushort type = WorldGen.TileIsExposedToAir(x, y) ? TileID.Sandstone : TileID.Sand;
							Main.tile[x, y].ResetToType(type);

							if (type == TileID.Sandstone)
							{
								if (WorldGen.genRand.NextBool(12))
								{
									WorldUtils.Gen(new(x, y), new Shapes.Tail(WorldGen.genRand.Next(4, 8), new Vector2D(0, WorldGen.genRand.Next(4, 16))), Actions.Chain(
										new Modifiers.IsNotSolid(),
										new Actions.SetTileKeepWall(TileID.Sandstone)
									));
								}
								else if (WorldGen.genRand.NextBool(7))
								{
									WorldGen.PlaceTile(x, y + 1, ModContent.TileType<LightShaft>(), true);
								}
							}

							return true;
						}),
						new Actions.PlaceWall(WallID.HardenedSand)
					));
				}

				return false;
			}, out _, b);
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

			int totalCount = i + 1;
			int numSkips = 0;

			for (int x = 0; x <= totalCount; x++)
			{
				float progress = (float)x / totalCount;

				if (totalCount == 1)
					progress = 0.5f;

				ZigguratRooms.BasicRoom r = SelectRoom(i + 1, bound, ref progress, numSkips);

				if (r is null)
				{
					numSkips++;
					continue; //Skip
				}

				int originX = (int)MathHelper.Lerp(innerBounds.Left + r.Bounds.Width / 2, innerBounds.Right - r.Bounds.Width / 2, progress);
				Point origin = new(originX, innerBounds.Bottom - r.Bounds.Height / 2);
				r.SetOrigin(origin);

				if (!rooms.Any(x => x.Intersects(r.Bounds, 2))) //Check if rooms would intersect
					rooms.Add(r);
			}
		}
	}

	/// <summary> Chooses the type of room to generate. </summary>
	/// <param name="layer"> The layer this room is being selected for, indexed by one. </param>
	/// <param name="bound"> The bounds of the layer to generate within. </param>
	/// <param name="progress"> Indicates the room number relative to the total that generate on a particular layer. This value influences the final vertical coordinate. </param>
	/// <param name="skips"> The number of skips that have happened in this layer. Returning null here causes a skip. </param>
	private static ZigguratRooms.BasicRoom SelectRoom(int layer, Rectangle bound, ref float progress, int skips)
	{
		int numLayers = TotalBounds.Count;
		var r = new ZigguratRooms.BasicRoom(bound);

		if (layer == numLayers && progress == 0) //Final layer
		{
			r = new ZigguratRooms.TreasureRoom(bound);
			progress = WorldGen.genRand.NextFloat();
		}
		else if (layer == 1)
		{
			r = new ZigguratRooms.EntranceRoom(bound);
		}
		else if (skips == 0 && WorldGen.genRand.NextBool(4))
			return null; //Randomly cause a skip

		return r;
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

		static bool TryLink(GenRoom a, GenRoom b)
		{
			foreach (var start in a.Links)
			{
				foreach (var end in b.Links)
				{
					if (AddHallway(start, end))
					{
						a.Links.Remove(start);
						//b.Links.Remove(end);

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
			var down = sloped ? new Point(entrance.X + Math.Abs(end.Y - start.Y) * startLink.Direction.X, end.Y) : new Point(entrance.X, end.Y); //Straight or diagonal down given enough space

			if (!Intersecting(entrance, down, exit) && Contains(entrance) && Contains(down))
			{
				BlockOut(start, entrance, HallwayWidth);
				BlockOut(entrance, down, HallwayWidth);

				BlockOut(down, exit, HallwayWidth);
				BlockOut(exit, end, HallwayWidth);

				if (!sloped) //Avoid placing platforms at sloped entrances
				{
					for (int i = 0; i < 2; i++)
					{
						var shelf = ((i == 0) ? entrance : down) + new Point(-2, 3);
						WorldUtils.Gen(shelf, new Shapes.Rectangle(HallwayWidth, 1), new Actions.PlaceTile(TileID.Platforms, 42));
					}
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

			WorldUtils.Gen(origin, new Shapes.Rectangle(width, width), Actions.Chain(
				new Actions.ClearTile(true).Output(shape),
				new Actions.SetLiquid(0, 0)
			));
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