#nullable enable

using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.WorldGeneration;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria.Map;

namespace SpiritReforged.Content.Forest.Cartography;

/// <summary>
/// Keeps a record of the map data as its seen by the server and allows for
/// syncing that data across clients.
/// </summary>
public sealed class MappingSystem : ModSystem
{
	/// <summary> Used to record whether a change has actually occured on the server map. </summary>
	[WorldBound]
	internal static bool MapUpdated;

	/// <summary> The map owned by the server and controlled using <see cref="CartographyTable"/>. </summary>
	[WorldBound(Manual = true)]
	internal static WorldMap? RecordedMap;

	/// <summary>
	/// Requests the server to send updated map data to the client and then sends
	///	over any changes made by the client.
	/// </summary>
	public static bool Sync(int requestingClient = -1)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
			return false;

		if (Main.netMode == NetmodeID.Server)
			SyncMapData.EnqueueMapData(RecordedMap, null);
		else
			SyncMapData.EnqueueMapData(Main.Map, RecordedMap);

		SyncMapData.SendQueuedData(requestingClient);
		new NotifyMapData().Send(ignoreClient: requestingClient);
		return true;
	}

	/// <summary> Sends <see cref="RecordedMap"/> data between client and server. </summary>
	internal sealed class SyncMapData : PacketData
	{
		public readonly record struct SparseEntry(ushort X, ushort Y, MapTile Tile);

		public enum DeltaMode : byte
		{
			// Sparse list of points, for when few tiles need updating.
			Sparse = 0,

			// Small, unfixed rectangular areas smaller than chunks which don't
			// need compression.
			Rectangle = 1,

			// Larger, fixed-sized chunks.
			Chunk = 2,
		}

		// Same as tile section dimensions.
		private const byte chunk_width = 200;
		private const byte chunk_height = 150;
		private const ushort chunk_area = chunk_width * chunk_height;

		private static readonly List<PacketData> packets = [];

		public DeltaMode Mode { get; set; }

		public Rectangle Area { get; set; }

		public byte ChunkX { get; }

		public byte ChunkY { get; }

		public Queue<SparseEntry> SparseEntries { get; private set; } = [];

		// row-major: y * width + x
		// interate y first for cache coherence, then x
		public MapTile[] ChunkData { get; private set; } = [];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MapTile GetChunkTile(MapTile[] data, int x, int y)
			=> data[y * chunk_width + x];

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			RecordedMap ??= new WorldMap(Main.maxTilesX, Main.maxTilesY);

			Mode = (DeltaMode)reader.ReadByte();

			switch (Mode)
			{
				case DeltaMode.Sparse:
					{
						int count = reader.ReadUInt16();
						for (int i = 0; i < count; i++)
						{
							ushort x = reader.ReadUInt16();
							ushort y = reader.ReadUInt16();
							MapTile tile = ReadTile(reader);
							UpdateTile(x, y, tile);
						}

						break;
					}

				case DeltaMode.Rectangle:
					{
						ushort x = reader.ReadUInt16();
						ushort y = reader.ReadUInt16();
						byte w = reader.ReadByte();
						byte h = reader.ReadByte();

						for (int dy = 0; dy < h; dy++)
						for (int dx = 0; dx < w; dx++)
						{
							ushort tx = (ushort)(x + dx);
							ushort ty = (ushort)(y + dy);
							MapTile tile = ReadTile(reader);
							UpdateTile(tx, ty, tile);
						}

						break;
					}

				case DeltaMode.Chunk:
					{
						ushort x = (ushort)(reader.ReadByte() * chunk_width);
						ushort y = (ushort)(reader.ReadByte() * chunk_height);

						for (int dy = 0; dy < chunk_height; dy++)
						for (int dx = 0; dx < chunk_width; dx++)
						{
							ushort tx = (ushort)(x + dx);
							ushort ty = (ushort)(y + dy);
							MapTile tile = ReadTile(reader);
							UpdateTile(tx, ty, tile);
						}

						break;
					}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MapTile ReadTile(BinaryReader r)
		{
			ushort type = r.ReadUInt16();
			byte light = r.ReadByte();
			byte color = r.ReadByte();
			return MapTile.Create(type, light, color);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void UpdateTile(ushort x, ushort y, MapTile tile)
		{
			// Never dim server light levels.
			if (tile.Light > RecordedMap![x, y].Light)
				RecordedMap.Update(x, y, tile.Light);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				Main.Map.SetTile(x, y, ref tile);
		}

		public override void OnSend(ModPacket packet)
		{
			packet.Write((byte)Mode);

			switch (Mode)
			{
				case DeltaMode.Sparse:
					{
						packet.Write((ushort)SparseEntries.Count);

						foreach (var entry in SparseEntries)
						{
							packet.Write(entry.X);
							packet.Write(entry.Y);
							WriteTile(packet, entry.Tile);
						}

						break;
					}

				case DeltaMode.Rectangle:
					{
						packet.Write((ushort)Area.X);
						packet.Write((ushort)Area.Y);
						packet.Write((byte)Area.Width);
						packet.Write((byte)Area.Height);

						for (int dy = 0; dy < Area.Height; dy++)
						for (int dx = 0; dx < Area.Width; dx++)
						{
							MapTile tile = GetChunkTile(ChunkData, dy, dx);
							WriteTile(packet, tile);
						}

						break;
					}

				case DeltaMode.Chunk:
					{
						packet.Write(ChunkX);
						packet.Write(ChunkY);

						for (int dy = 0; dy < chunk_height; dy++)
						for (int dx = 0; dx < chunk_width; dx++)
						{
							MapTile tile = GetChunkTile(ChunkData, dx, dy);
							WriteTile(packet, tile);
						}

						break;
					}
			}
		}

		private static void WriteTile(ModPacket packet, MapTile tile)
		{
			packet.Write(tile.Type);
			packet.Write(tile.Light);
			packet.Write(tile.Color);
		}

		// Potential improvements:
		// - offload sparse list to a static field that gets re-used each time
		//   instead of allocating (fixed chunk_area),
		// - look into keeping a sparse list for multiple chunks if there is
		//   more sparse data,
		// - consider more than just light level for syncing?
		public static void EnqueueMapData(WorldMap? map, WorldMap? comparisonMap)
		{
			if (map is null)
				return;

			int width = Main.maxTilesX;
			int height = Main.maxTilesY;

			// Process in chunks.  The actual data does not need to be sent in
			// fixed chunks, but it's preferred for efficient packing.
			for (int cy = 0; cy < height; cy += chunk_height)
			for (int cx = 0; cx < width; cx += chunk_width)
				{
					var chunkRect = new Rectangle(
						cx,
						cy,
						Math.Min(chunk_width, width - cx),
						Math.Min(chunk_height, height - cy)
					);

					bool changed = false;
					int diffCount = 0;
					var sparse = new List<SparseEntry>();

					for (int dy = 0; dy < chunkRect.Height; dy++)
					for (int dx = 0; dx < chunkRect.Width; dx++)
					{
						ushort tx = (ushort)(chunkRect.X + dx);
						ushort ty = (ushort)(chunkRect.Y + dy);
						MapTile currentTile = map[tx, ty];
						MapTile? compareTile = comparisonMap?[tx, ty];

						if (!compareTile.HasValue || currentTile.Light >= compareTile?.Light)
						{
							changed = true;
							diffCount++;
							sparse.Add(new SparseEntry(tx, ty, currentTile));
						}
					}

					if (!changed)
						continue;

					// TODO: Look into each value and tweak for efficiency?
				}

			packets.Add(new CommitMapData());
		}

		public static void SendQueuedData(int requestingClient = -1)
		{
			foreach (var packetData in packets)
				packetData.Send(ignoreClient: requestingClient);

			packets.Clear();
		}
	}

	/// <summary> Marks the end of a stream of map syncing packets and commits the data. </summary>
	internal sealed class CommitMapData : PacketData
	{
		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				Sync(requestingClient: whoAmI);
			}
			else
			{
				Main.refreshMap = true;

				string key = "Mods.SpiritReforged.Misc." + (MapUpdated ? "ShareAndUpdateMap" : "ShareMap");
				Main.NewText(Language.GetTextValue(key), new Color(255, 240, 20));

				MapUpdated = false;
			}
		}

		public override void OnSend(ModPacket modPacket) { }
	}

	/// <summary> Simply notifies everybody of updated map data on the server. </summary>
	internal sealed class NotifyMapData : PacketData
	{
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