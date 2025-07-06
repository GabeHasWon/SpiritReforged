using SpiritReforged.Content.Desert.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ZigguratMicropass : Micropass
{
	public enum RoomVariant : byte
	{
		Empty,
		Gilded,
		Count,
		Entry,
		Scarab,
	}

	public class ZigguratConfiguration
	{
		public Dictionary<Point16, PlacedRoom> Rooms = [];
		public int[] LayerRoomCounts = new int[5];
		public int[] RoomPath = new int[5];

		public ZigguratConfiguration(PlacedRoom[] rooms, int[] layerRoomCounts)
		{
			foreach (var room in rooms)
				Rooms.Add(room.Position, room);

			LayerRoomCounts = layerRoomCounts;
			RoomPath = [0, -1, -1, -1, 0];
		}
	}

	public record PlacedRoom(Rectangle Room, RoomVariant Variant, Point16 Position, List<Point16> Links);

	public override string WorldGenName => "Ziggurat";

	private static int Spacing => 18;

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Water Chests");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		int x = WorldGen.genRand.Next(GenVars.UndergroundDesertLocation.Left, GenVars.UndergroundDesertLocation.Right);
		int y = GenVars.UndergroundDesertLocation.Y - 40;

		if (!WorldUtils.Find(new Point(x, y), new Searches.Down(1500).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return; // ?? big hole where the desert is?

		(x, y) = (foundPos.X, foundPos.Y);
		GenerateZiggurat(x, y);
	}

	internal static void GenerateZiggurat(int x, int y)
	{
		HashSet<Point16> allData = [];
		var configuration = GenerateRooms(x, y, allData);
		HorizontalConnectRooms(configuration, allData);
		ShapeZiggurat(x, y, configuration, allData);
	}

	private static void ShapeZiggurat(int x, int y, ZigguratConfiguration config, HashSet<Point16> allData)
	{
		var areas = new Vector4[5];
		int width = 35;

		y -= 3;

		for (int i = 0; i < 5; ++i)
		{
			areas[i] = new Vector4(x - width, y, x + width, y + 40);
			y += Spacing - 2;
			width += Spacing;
		}

		for (int i = 0; i < 5; ++i)
		{
			Vector4 bounds = areas[i];

			if (i != 4)
				bounds.W = areas[i + 1].Y;
			else
				bounds.W -= 2;

			BlockOutArea(allData, bounds);
		}
	}

	private static void BlockOutArea(HashSet<Point16> allData, Vector4 bounds)
	{
		for (int x = (int)bounds.X; x < bounds.Z; ++x)
		{
			for (int y = (int)bounds.Y; y < bounds.W; ++y)
			{
				if (allData.Contains(new Point16(x, y)))
					continue;

				Tile tile = Main.tile[x, y];
				tile.TileType = (ushort)ModContent.TileType<GildedSandstone>();
				tile.HasTile = true;
			}
		}
	}

	public static ZigguratConfiguration GenerateRooms(int x, int y, HashSet<Point16> list)
	{
		List<PlacedRoom> rooms = [GenerateStartRoom(x, y, list)];
		int placeY = y;
		int[] roomCounts = [1, 0, 0, 0, 1];

		for (int layer = 0; layer < 3; ++layer)
		{
			int roomCount = layer + 2;
			int roomsSkipped = 0;

			roomCounts[layer + 1] = roomCount;
			placeY += Spacing;

			for (int repeat = 0; repeat < roomCount; ++repeat)
			{
				if (roomsSkipped < roomCount - 2 && WorldGen.genRand.NextBool(3))
				{
					roomsSkipped++;
					continue;
				}

				int width = WorldGen.genRand.Next(17, 25);
				int height = WorldGen.genRand.Next(12, 15);

				if (WorldGen.genRand.NextBool(4))
					width *= 2;

				int yOffset = placeY - y;
				int placeX = (int)MathHelper.Lerp(x - yOffset, x + yOffset, repeat / (float)(roomCount - 1));
				placeX -= width / 2;

				ShapeData roomData = new();
				WorldUtils.Gen(new Point(placeX, placeY), new Shapes.Rectangle(width, height), new Actions.PlaceWall(WallID.Sandstone).Output(roomData));
				rooms.Add(new PlacedRoom(new Rectangle(placeX, placeY, width, height), RoomVariant.Empty, new Point16(repeat, layer + 1), []));

				foreach (var point in roomData.GetData())
					list.Add(new Point16(point.X + placeX, point.Y + placeY));
			}
		}

		placeY += Spacing;
		int scarabWidth = 60;
		ShapeData data = new();
		WorldUtils.Gen(new Point(x - scarabWidth / 2, placeY), new Shapes.Rectangle(scarabWidth, 18), new Actions.PlaceWall(WallID.Sandstone).Output(data));
		rooms.Add(new PlacedRoom(new Rectangle(x - scarabWidth / 2, placeY, scarabWidth, 18), RoomVariant.Empty, new Point16(0, 4), []));

		foreach (var point in data.GetData())
			list.Add(new Point16(point.X + x - scarabWidth / 2, point.Y + placeY));

		return new([.. rooms], roomCounts);
	}

	public static PlacedRoom GenerateStartRoom(int x, int y, HashSet<Point16> walls)
	{
		int width = 30 + WorldGen.genRand.Next(-1, 4) * 2;
		int height = 13;
		HashSet<Point16> pos = [];

		x -= width / 2;

		for (int i = x; i <= x + width; i++)
		{
			for (int j = y - 1; j <= y + height; j++)
			{
				Tile tile = Main.tile[i, j];

				if (j >= y)
				{
					if (j != y && j != y + height && i != x && i != x + width)
					{
						tile.WallType = WallID.Sandstone;
						walls.Add(new Point16(i, j));
					}

					pos.Add(new Point16(i, j));
				}
				else
				{
					if (i < x + 3 || i > x + width - 3)
					{
						tile.HasTile = true;
						tile.TileType = (ushort)ModContent.TileType<GildedSandstone>();
					}

					if (i >= x + width / 2 - 1 && i <= x + width / 2 + 1 && j == y - 1)
					{
						tile.HasTile = true;
						tile.TileType = TileID.GoldBrick;
					}

					if (i == x + width / 2 && j == y - 1)
					{
						Tile above = Main.tile[i, j - 1];
						above.HasTile = true;
						above.TileType = TileID.GoldBrick;
					}
				}
			}
		}

		return new PlacedRoom(new Rectangle(x, y - 1, width, height + 1), RoomVariant.Entry, new Point16(0, 0), []);
	}

	internal static void HorizontalConnectRooms(ZigguratConfiguration config, HashSet<Point16> allData)
	{
		for (int i = 0; i < 5; ++i)
		{
			int layer = i;

			List<PlacedRoom> layerRooms = [];
			List<PlacedRoom> belowRooms = [];

			foreach (PlacedRoom room in config.Rooms.Values)
			{
				if (room.Position.Y == layer)
					layerRooms.Add(room);
				else if (room.Position.Y == layer + 1)
					belowRooms.Add(room);
			}

			PlacedRoom pathRoom = WorldGen.genRand.Next(layerRooms);
			config.RoomPath[layer] = pathRoom.Position.X;

			layerRooms = [.. layerRooms.OrderBy(x => x.Position.X)];

			for (int j = 0; j < layerRooms.Count - 1; ++j)
				PlaceHallway(layerRooms[j], layerRooms[j + 1], allData, false);

			if (belowRooms.Count > 0)
				PlaceHallway(WorldGen.genRand.Next(layerRooms), WorldGen.genRand.Next(belowRooms), allData, true);
		}
	}

	private static void PlaceHallway(PlacedRoom leftRoom, PlacedRoom rightRoom, HashSet<Point16> allData, bool checkAbove)
	{
		Vector2 connection = leftRoom.Room.BottomRight();

		if (checkAbove)
			connection = leftRoom.Room.Center().X < rightRoom.Room.Center().X ? connection : leftRoom.Room.BottomLeft();

		float dist = connection.Distance(rightRoom.Room.BottomLeft());

		for (int i = 0; i < dist; ++i)
		{
			var pos = Vector2.Lerp(connection, rightRoom.Room.BottomLeft(), i / (float)dist).ToPoint();

			for (int x = pos.X - 2; x <= pos.X + 2; ++x)
			{
				for (int y = pos.Y; y > pos.Y - 5; --y)
				{
					Tile tile = Main.tile[x, y];
					tile.HasTile = false;
					tile.WallType = WallID.SandstoneEcho;

					allData.Add(new Point16(x, y));
				}
			}
		}
	}
}
