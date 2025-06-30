using SpiritReforged.Content.Desert.Tiles;
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

		public ZigguratConfiguration(PlacedRoom[] rooms, int[] layerRoomCounts)
		{
			foreach (var room in rooms)
				Rooms.Add(room.Position, room);

			LayerRoomCounts = layerRoomCounts;
		}
	}

	public record PlacedRoom(Rectangle Room, RoomVariant Variant, Point16 Position, List<Point16> Links);

	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Water Chests");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		int x = WorldGen.genRand.Next(GenVars.UndergroundDesertLocation.Left, GenVars.UndergroundDesertLocation.Right);
		int y = GenVars.UndergroundDesertLocation.Y - 40;

		if (!WorldUtils.Find(new Point(x, y), new Searches.Down(1500).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return; // ?? big hole where the desert is?

		(x, y) = (foundPos.X, foundPos.Y);

		ShapeData allData = new();
		var configuration = GenerateRooms(x, y, allData);
		HorizontalConnectRooms(configuration);
	}

	public static ZigguratConfiguration GenerateRooms(int x, int y, ShapeData data)
	{
		List<PlacedRoom> rooms = [GenerateStartRoom(x, y)];
		int placeY = y;
		int[] roomCounts = [1, 0, 0, 0, 1];

		for (int layer = 0; layer < 3; ++layer)
		{
			int roomCount = layer + 2;
			int roomsSkipped = 0;

			roomCounts[layer + 1] = roomCount;
			placeY += 20;

			for (int repeat = 0; repeat < roomCount; ++repeat)
			{
				if (roomsSkipped < roomCount && WorldGen.genRand.NextBool(3))
				{
					roomsSkipped++;
					continue;
				}

				int width = 20;
				int yOffset = placeY - y;
				int placeX = (int)MathHelper.Lerp(x - yOffset, x + yOffset, repeat / (float)(roomCount - 1));
				placeX -= width / 2;

				WorldUtils.Gen(new Point(placeX, placeY), new Shapes.Rectangle(width, 12), new Actions.PlaceWall(WallID.Sandstone).Output(data));
				rooms.Add(new PlacedRoom(new Rectangle(x, y, width, 12), RoomVariant.Empty, new Point16(repeat, layer + 1), []));
			}
		}

		placeY += 20;
		int scarabWidth = 40;
		WorldUtils.Gen(new Point(x - scarabWidth / 2, placeY), new Shapes.Rectangle(scarabWidth, 22), new Actions.PlaceWall(WallID.Sandstone).Output(data));
		rooms.Add(new PlacedRoom(new Rectangle(x, y, scarabWidth, 12), RoomVariant.Empty, new Point16(0, 4), []));

		return new([.. rooms], roomCounts);
	}

	public static PlacedRoom GenerateStartRoom(int x, int y)
	{
		int width = 20 + WorldGen.genRand.Next(-1, 4) * 2;
		int height = 11;
		HashSet<Point16> pos = [];

		x -= width / 2;

		for (int i = x; i <= x + width; i++)
		{
			for (int j = y - 1; j <= y + height; j++)
			{
				Tile tile = Main.tile[i, j];

				if (j >= y)
				{
					if (j < y + 3 || j > y + height - 3 || i < x + 3 || i > x + width - 3)
					{
						tile.HasTile = true;
						tile.TileType = (ushort)ModContent.TileType<GildedSandstone>();
					}

					if (j != y && j != y + height && i != x && i != x + width)
					{
						tile.WallType = WallID.Sandstone;
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

	internal static void HorizontalConnectRooms(ZigguratConfiguration config)
	{
		for (int i = 0; i < 3; ++i)
		{
			if (config.LayerRoomCounts[i + 1] <= 1)
				continue;
		}
	}
}
