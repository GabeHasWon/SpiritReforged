using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.Map;

namespace SpiritReforged.Content.Forest.Cartography;

public class MappingSystem : ModSystem
{
	/// <summary> Used to record whether a change has actually occured on the server map. </summary>
	[WorldBound]
	internal static bool MapUpdated;

	/// <summary> The map owned by the server and controlled using <see cref="CartographyTable"/>. </summary>
	[WorldBound(Manual = true)]
	internal static WorldMap RecordedMap = null;

	/// <summary> Syncs your map with the server. Should only be called on multiplayer clients. </summary>
	public static void SetMap()
	{
		EnqueueMap(Main.Map, RecordedMap);

		if (SyncMapData.Queue.Count == 0) //Player has no data to send
		{
			Main.NewText(Language.GetTextValue("Mods.SpiritReforged.Misc.UnchangedMap"), new Color(255, 240, 20));
			MapUpdated = false; //Reset MapUpdated, just in case

			return;
		}

		while (SyncMapData.Queue.Count > 0)
		{
			new SyncMapData().Send();
		}
	}

	internal static void EnqueueMap(WorldMap map, WorldMap comparison)
	{
		for (int x = 0; x < map.MaxWidth; x++)
		{
			for (int y = 0; y < map.MaxHeight; y++)
			{
				var t = map[x, y];

				if (t.Light == 0 || comparison is WorldMap cMap && cMap[x, y].Light >= t.Light)
					continue; //Avoid sending redundant data by referencing the opposite map

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

		/// <summary> The total number of bytes sent in one iteration (a single tile). </summary>
		private const byte SequenceSize = 8;
		/// <summary> The total number of iterations allowed for a single packet. </summary>
		private const int CountLimit = ushort.MaxValue / SequenceSize - 1;

		public static readonly Queue<QueueData> Queue = [];

		public SyncMapData() { }

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			ushort count = reader.ReadUInt16();
			bool final = count < CountLimit;

			for (int i = 0; i < count; i++)
			{
				ushort x = reader.ReadUInt16();
				ushort y = reader.ReadUInt16();

				ushort type = reader.ReadUInt16();
				byte light = reader.ReadByte();
				byte color = reader.ReadByte();

				var t = MapTile.Create(type, light, color);

				//Set the server-owned map on ALL sides to protect against synchronizing redundant data in the future
				RecordedMap ??= new(Main.maxTilesX, Main.maxTilesY);

				//Never dim the server map light levels
				if (light > RecordedMap[x, y].Light)
					RecordedMap.SetTile(x, y, ref t);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					Main.Map.SetTile(x, y, ref t);
			}

			if (final)
			{
				if (Main.netMode == NetmodeID.Server)
				{
					EnqueueMap(RecordedMap, null);
					while (Queue.Count > 0)
					{
						new SyncMapData().Send(toClient: whoAmI); //Relay back to the initiator
					}

					new NotifyMapData().Send(ignoreClient: whoAmI);
				}
				else
				{
					Main.refreshMap = true;

					string key = "Mods.SpiritReforged.Misc." + (MapUpdated ? "ShareAndUpdateMap" : "ShareMap");
					Main.NewText(Language.GetTextValue(key), new Color(255, 240, 20));

					MapUpdated = false;
				}
			}
		}

		public override void OnSend(ModPacket modPacket)
		{
			ushort count = (ushort)Math.Min(Queue.Count, CountLimit); //Restricts packet size to avoid hitting the limit
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

	/// <summary> Simply notifies everybody of updated map data on the server. </summary>
	internal class NotifyMapData : PacketData
	{
		public NotifyMapData() { }

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				new NotifyMapData().Send();
				return;
			}

			MapUpdated = true;
		}

		public override void OnSend(ModPacket modPacket) { }
	}
}