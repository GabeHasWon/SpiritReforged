using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.Map;

namespace SpiritReforged.Content.Forest.Cartography;

public class MappingSystem : ModSystem
{
	internal static WorldMap RecordedMap;

	public override void ClearWorld() => RecordedMap = null;

	public static void SetMap()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		for (int x = 0; x < Main.Map.MaxWidth; x++)
		{
			for (int y = 0; y < Main.Map.MaxHeight; y++)
			{
				var t = Main.Map[x, y];
				
				if (t.Light != 0)
					SyncMapData.Queue.Enqueue(new((ushort)x, (ushort)y, t));
			}
		}

		while (SyncMapData.Queue.Count > 0)
		{
			new SyncMapData().Send();
		}

		Main.refreshMap = true;
	}
}

internal class SyncMapData : PacketData
{
	public readonly record struct QueueData(ushort X, ushort Y, MapTile Tile)
	{
		public readonly ushort X = X;
		public readonly ushort Y = Y;
		public readonly MapTile Tile = Tile;
	}

	public static readonly Queue<QueueData> Queue = [];

	public SyncMapData() { }

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		ushort count = reader.ReadUInt16();
		for (int i = 0; i < count; i++)
		{
			ushort x = reader.ReadUInt16();
			ushort y = reader.ReadUInt16();

			ushort type = reader.ReadUInt16();
			byte light = reader.ReadByte();
			byte color = reader.ReadByte();

			var t = MapTile.Create(type, light, color);

			if (Main.netMode == NetmodeID.Server)
			{
				MappingSystem.RecordedMap ??= new(Main.maxTilesX, Main.maxTilesY);
				MappingSystem.RecordedMap.SetTile(x, y, ref t);

				Queue.Enqueue(new(x, y, t)); //Enqueue again because the server needs to relay
			}
			else
			{
				Main.Map.SetTile(x, y, ref t);
			}
		}

		if (Main.netMode == NetmodeID.Server)
			new SyncMapData().Send(); //Relay to all clients
	}

	public override void OnSend(ModPacket modPacket)
	{
		ushort count = (ushort)Math.Min(150, Queue.Count); //Restricts packet size to avoid hitting the limit
		ushort currentCount = 0;

		modPacket.Write(count);

		foreach (var data in Queue)
		{
			if (currentCount++ > count)
				break;

			modPacket.Write(data.X);
			modPacket.Write(data.Y);

			var t = data.Tile;
			modPacket.Write(t.Type);
			modPacket.Write(t.Light);
			modPacket.Write(t.Color);
		}

		for (int i = 0; i < count; i++)
			Queue.Dequeue();
	}
}