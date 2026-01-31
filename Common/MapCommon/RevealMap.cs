using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;
using Terraria.Map;

namespace SpiritReforged.Common.MapCommon;

/// <summary> Used by the Cartographer to reveal points of interest on the map. </summary>
internal class RevealMap
{
	public enum MapSyncId : byte
	{
		DrawMap,
	}

	/// <summary> Reveals the map around the given tile coordinates and handles syncing automatically. </summary>
	/// <param name="x"> The X coordinate. </param>
	/// <param name="y"> The Y coordinate. </param>
	/// <param name="radius"> The radius to reveal. </param>
	public static void Reveal(int x, int y, int radius)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendData(MessageID.TileSection, -1, -1, null, x - radius, y - radius, radius * 2, radius * 2);
			new RevealMapData((byte)MapSyncId.DrawMap, new Point16(x, y), (short)radius).Send();
		}
		else
		{
			DrawMap(x, y, radius);
		}
	}

	private static void DrawMap(int x, int y, int radius)
	{
		var tile = MapHelper.CreateMapTile(x, y, 255);

		for (int i = x - radius / 2; i <= x + radius / 2; ++i)
		{
			for (int j = y - radius / 2; j <= y + radius / 2; ++j)
			{
				if (!WorldGen.InWorld(i, j))
					continue;

				float dist = 1 - Vector2.Distance(new Vector2(x, y), new Vector2(i, j)) / (radius * 0.5f);
				byte light = WorldGen.SolidTile(i, j) ? (byte)(255 * Math.Max(dist, 0)) : (byte)0;

				if (light <= 0 || Main.Map[i, j].Light > 0)
					continue;

				SetTileAt(i, j, (byte)(215 + Main.rand.Next(40)), tile);
			}
		}

		Main.refreshMap = true;
	}

	private static void SetTileAt(int x, int y, byte light, MapTile tile)
	{
		tile.Light = light;
		tile.Type = 34;
		tile.IsChanged = false;
		Main.Map.SetTile(x, y, ref tile);
	}

	public static void RecieveSync(MapSyncId id, BinaryReader reader)
	{
		switch (id)
		{
			case MapSyncId.DrawMap:
				short x = reader.ReadInt16();
				short y = reader.ReadInt16();
				short size = reader.ReadInt16();

				if (Main.netMode == NetmodeID.Server)
				{
					foreach (var plr in Main.ActivePlayers) // This is a little hacky lol
						RemoteClient.CheckSection(plr.whoAmI, new Vector2(x, y).ToWorldCoordinates(), 2);

					new RevealMapData((byte)id, new Point16(x, y), size).Send();
				}
				else
					DrawMap(x, y, size);

				break;
		}
	}
}

internal class RevealMapData : PacketData
{
	private readonly byte _syncType;
	private readonly Point16 _point;
	private readonly short _size;

	public RevealMapData() { }
	public RevealMapData(byte syncType, Point16 point, short size)
	{
		_syncType = syncType;
		_point = point;
		_size = size;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		var syncType = (RevealMap.MapSyncId)reader.ReadByte();
		RevealMap.RecieveSync(syncType, reader);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_syncType);
		modPacket.Write(_point.X);
		modPacket.Write(_point.Y);
		modPacket.Write(_size);
	}
}
