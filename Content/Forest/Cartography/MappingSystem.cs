using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.Map;

namespace SpiritReforged.Content.Forest.Cartography;

public class MappingSystem : ModSystem
{
	public static bool MapUpdated { get; private set; }
	[WorldBound(Manual = true)]
	internal static WorldMap RecordedMap;

	/// <summary> Syncs your map with the server. Should only be called on multiplayer clients. </summary>
	public static void SetMap()
	{
		EnqueueMap(Main.Map);
		while (SyncMapData.Queue.Count > 0)
		{
			new SyncMapData().Send();
		}

		new NotifyMapData().Send();
		MapUpdated = false;
	}

	internal static void EnqueueMap(WorldMap map)
	{
		for (int x = 0; x < map.MaxWidth; x++)
		{
			for (int y = 0; y < map.MaxHeight; y++)
			{
				var t = map[x, y];

				if (t.Light != 0)
					SyncMapData.Queue.Enqueue(new((ushort)x, (ushort)y, t));
			}
		}
	}

	/// <summary> Sends <see cref="RecordedMap"/> data between client and server. </summary>
	internal class SyncMapData : PacketData
	{
		public readonly record struct QueueData(ushort X, ushort Y, MapTile Tile)
		{
			public readonly ushort X = X;
			public readonly ushort Y = Y;
			public readonly MapTile Tile = Tile;
		}

		private const int CountLimit = 150;
		public static readonly Queue<QueueData> Queue = [];

		public SyncMapData() { }

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			ushort count = reader.ReadUInt16();
			bool final = count < CountLimit; //Whether a client is requesting map data from the server

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
					RecordedMap ??= new(Main.maxTilesX, Main.maxTilesY);

					if (light > RecordedMap[x, y].Light)
						RecordedMap.SetTile(x, y, ref t); //Never dim the server map light levels
				}
				else
				{
					Main.Map.SetTile(x, y, ref t);

					if (final)
						Main.refreshMap = true;
				}
			}

			if (final && Main.netMode == NetmodeID.Server)
			{
				EnqueueMap(RecordedMap);
				while (Queue.Count > 0)
				{
					new SyncMapData().Send(toClient: whoAmI); //Relay back to the initiator
				}
			}
		}

		public override void OnSend(ModPacket modPacket)
		{
			ushort count = (ushort)Math.Min(CountLimit, Queue.Count); //Restricts packet size to avoid hitting the limit
			ushort currentCount = 0;

			modPacket.Write(count);

			foreach (var data in Queue)
			{
				if (++currentCount > count)
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

	/// <summary> Notifies all clients whether map data was updated on the server. </summary>
	internal class NotifyMapData : PacketData
	{
		public NotifyMapData() { }

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode == NetmodeID.Server)
				new NotifyMapData().Send(ignoreClient: whoAmI);
			else
				MapUpdated = true;
		}

		public override void OnSend(ModPacket modPacket) { }
	}
}