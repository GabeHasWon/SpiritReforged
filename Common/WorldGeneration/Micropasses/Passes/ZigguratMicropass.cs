using SpiritReforged.Common.WorldGeneration.GenerationModifiers;
using SpiritReforged.Common.WorldGeneration.Noise;
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
		Scarab,
	}

	public readonly record struct PlacedRoom(Rectangle Room, RoomVariant Variant);

	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Water Chests");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		int x = WorldGen.genRand.Next(GenVars.UndergroundDesertLocation.Left, GenVars.UndergroundDesertLocation.Right);
		int y = GenVars.UndergroundDesertLocation.Y - 40;

		if (!WorldUtils.Find(new Point(x, y), new Searches.Down(500), out Point foundPos))
			return; // ?? big hole where the desert is?

		(x, y) = (foundPos.X, foundPos.Y);

		List<PlacedRoom> rooms = GenerateBase(x, y);
		PopulateRooms(rooms);
	}

	internal static void PopulateRooms(List<PlacedRoom> rooms)
	{
		FastNoiseLite noise = new();

		foreach (PlacedRoom room in rooms)
		{
			if (room.Variant == RoomVariant.Empty)
				PopulateEmptyRoom(room, noise);
			else if (room.Variant == RoomVariant.Gilded)
				PopulateGildedRoom(room, noise);
		}
	}

	private static void PopulateGildedRoom(PlacedRoom room, FastNoiseLite noise)
	{
		AddSandBase(room, noise);
		AddGildedPillars(room);
		AddCoinPiles(room);
	}

	private static void AddCoinPiles(PlacedRoom room)
	{
		int count = WorldGen.genRand.Next(3, 6);

		while (count > 0)
		{
			Point16 pos = new(WorldGen.genRand.Next(room.Room.Left, room.Room.Right), WorldGen.genRand.Next(room.Room.Top, room.Room.Bottom));
			Tile tile = Main.tile[pos.X, pos.Y + 1];
			
			if (!WorldGen.SolidTile(pos.X, pos.Y) && WorldGen.SolidTile(pos.X, pos.Y + 1) || tile.HasTile && tile.TileType is TileID.CopperCoinPile or TileID.SilverCoinPile)
			{
				tile = Main.tile[pos];
				tile.TileType = !WorldGen.genRand.NextBool(8) ? TileID.CopperCoinPile : TileID.SilverCoinPile;
				tile.HasTile = true;
				count--;
			}
		}
	}

	private static void AddGildedPillars(PlacedRoom room)
	{
		int count = 2 + WorldGen.genRand.Next(3);

		for (int i = 0; i < count; ++i)
		{
			int x = (int)MathHelper.Lerp(room.Room.Left, room.Room.Right, (i + 1) / (float)count) + WorldGen.genRand.Next(-2, 3);

			for (int y = room.Room.Bottom; y > room.Room.Top; y--)
			{
				if (!WorldGen.genRand.NextBool(7))
				{
					Tile tile = Main.tile[x, y];
					tile.WallType = WallID.GoldBrick;
				}
			}
		}
	}

	private static void PopulateEmptyRoom(PlacedRoom room, FastNoiseLite noise)
	{
		AddSandBase(room, noise);
		EmptyRoomCrumbledPillars(room);
	}

	private static void AddSandBase(PlacedRoom room, FastNoiseLite noise)
	{
		int yBase = room.Room.Bottom - WorldGen.genRand.Next(2, 4);

		for (int i = room.Room.Left; i < room.Room.Right; ++i)
		{
			int height = (int)(Math.Abs(noise.GetNoise(i * 2, yBase)) * 6);

			for (int j = yBase; j > yBase - height; --j)
			{
				Tile tile = Main.tile[i, j];

				if (tile.HasTile)
					continue;

				tile.HasTile = true;
				tile.TileType = TileID.Sand;
			}
		}
	}

	private static void EmptyRoomCrumbledPillars(PlacedRoom room)
	{
		int count = WorldGen.genRand.Next(0, 6);

		for (int i = 0; i < count; ++i)
		{
			bool vert = WorldGen.genRand.NextBool();
			Point origin;

			if (vert)
			{
				int y = WorldGen.genRand.NextBool() ? room.Room.Top + 2 : room.Room.Bottom - 2;
				origin = new Point((int)MathHelper.Lerp(room.Room.Left + 3, room.Room.Right - 3, WorldGen.genRand.NextFloat()), y);
			}
			else
			{
				int x = WorldGen.genRand.NextBool() ? room.Room.Left + 3 : room.Room.Right - 3;
				origin = new Point(x, (int)MathHelper.Lerp(room.Room.Top + 2, room.Room.Bottom - 2, WorldGen.genRand.NextFloat()));
			}

			int dir = Math.Sign(room.Room.Center.X - origin.X);
			float yOffset = WorldGen.genRand.NextFloat(0.2f, 1f) * (WorldGen.genRand.NextBool() ? -1 : 1);
			int width = WorldGen.genRand.Next(4, (int)Math.Max(room.Room.Width * 0.5f, 6));
			origin.X += dir;

			for (int x = origin.X; x != origin.X + dir * width; x += dir)
			{
				for (int y = origin.Y - 3; y < origin.Y + 2; ++y)
				{
					if (WorldGen.genRand.NextBool(5))
						continue;

					Tile tile = Main.tile[x, y + (int)(yOffset * (x - origin.X))];
					tile.HasTile = y > origin.Y - 1;
					tile.TileType = TileID.Sandstone;

					if (WorldGen.genRand.NextBool(3) && tile.HasTile)
						tile.TileType = TileID.Sand;
				}
			}
		}
	}

	public static List<PlacedRoom> GenerateBase(int x, int y)
	{
		int width = 14;
		int height = 9;
		List<PlacedRoom> rooms = [];

		for (int i = 0; i < 5; ++i)
		{
			GenerateRoom(x, ref y, width, height, rooms, i == 4);

			float range = WorldGen.genRand.NextFloat();
			width += (int)MathHelper.Lerp(10, 16, range);
			height += (int)MathHelper.Lerp(3, 5, range);
		}

		return rooms;
	}

	private static void GenerateRoom(int x, ref int y, int width, int height, List<PlacedRoom> rooms, bool scarabRoom)
	{
		x -= width / 2 + WorldGen.genRand.Next(-1, 2);
		ShapeData data = new();
		rooms.Add(new(new Rectangle(x, y, width, height), scarabRoom ? RoomVariant.Scarab : (RoomVariant)WorldGen.genRand.Next((int)RoomVariant.Count)));

		WorldUtils.Gen(new Point(x, y), new Shapes.Rectangle(width, height), Actions.Chain( // Clear & add walls
		    new Actions.Clear().Output(data),
			new Actions.PlaceWall(WallID.Sandstone)
		));

		WorldUtils.Gen(new Point(x, y), new ModShapes.InnerOutline(data, true), Actions.Chain( // Add tile walls & clear walls on border
			new Actions.ClearWall(),
			new Modifiers.Expand(1),
			new Actions.SetTile((ushort)ModContent.TileType<GildedSandstone>(), true),
			new NotOpenOrWalled(),
			new Actions.PlaceWall(WallID.Sandstone)
		));

		GenVars.structures?.AddProtectedStructure(new Rectangle(x, y, width, height), 6);
		y += height - 1;
	}
}
