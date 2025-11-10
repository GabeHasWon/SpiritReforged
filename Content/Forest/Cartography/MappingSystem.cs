#nullable enable

using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.WorldGeneration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

	private static readonly ConcurrentQueue<(byte[] compressed, int whoAmI)> pending_packets = [];
	private const int max_workers = 4;
	private static int activeWorkers;

	// Number of asynchronously processed packets we're waiting on before
	// we can consume a commit packet.
	private static int unhandledPacketCount;

	// Whether we need to handle a commit packet this frame.
	private static volatile int handleCommit = -2;

	/// <summary>
	/// Syncs map data from a multiplayer client to the server.
	/// </summary>
	public static bool Sync()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return false;

		Task.Run(async () =>
		{
			SyncMapData.EnqueueMapData(Main.Map, RecordedMap);
			await SyncMapData.SendQueuedDataAsync();
		});
		return true;
	}

	// Arbitrary update hook ran on multiplayer clients and the server.
	public override void PreUpdateDusts()
	{
		// Probably won't ever matter, but important to immediately cache the
		// value here since it's volatile.
		int whoAmI = handleCommit;
		if (whoAmI == -2)
			return;

		handleCommit = -2;

		if (Main.netMode == NetmodeID.Server)
		{
			Task.Run(async () =>
			{
				SyncMapData.EnqueueMapData(RecordedMap, null);

				// Relay updated map data back to the initiator.
				await SyncMapData.SendQueuedDataAsync(toClient: whoAmI);

				// Notify all other clients of the updated map data.
				new NotifyMapData().Send(ignoreClient: whoAmI);
			});
		}
		else
		{
			Main.refreshMap = true;

			string key = "Mods.SpiritReforged.Misc." + (MapUpdated ? "ShareAndUpdateMap" : "ShareMap");
			Main.NewText(Language.GetTextValue(key), new Color(255, 240, 20));

			MapUpdated = false;
		}
	}

	public override void Unload()
	{
		pending_packets.Clear();
	}

	/// <summary> Sends <see cref="RecordedMap"/> data between client and server. </summary>
	internal sealed class SyncMapData : PacketData
	{
		public readonly record struct SparseEntry(ushort X, ushort Y, MapTile Tile);

		public enum DeltaMode : byte
		{
			// Sparse list of points, for when few tiles need updating.
			Sparse = 0,

			// Larger, fixed-sized rectangles which encode base coordinates
			// followed by a continguous block of data for the area.
			Chunk = 1,
		}

		// Same as tile section dimensions.
		private const byte chunk_width = 200;
		private const byte chunk_height = 150;
		private const ushort chunk_area = chunk_width * chunk_height;

		private static readonly List<PacketData> packets = [];

		public DeltaMode Mode { get; private set; }

		public byte ChunkX { get; private set; }

		public byte ChunkY { get; private set; }

		public List<SparseEntry> SparseEntries { get; private set; } = [];

		// row-major: y * width + x
		// interate y first for cache coherence, then x
		public MapTile[] ChunkData { get; private set; } = [];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ref MapTile GetChunkTile(MapTile[] data, int x, int y)
			=> ref data[y * chunk_width + x];

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			RecordedMap ??= new WorldMap(Main.maxTilesX, Main.maxTilesY);

			ushort compressedLength = reader.ReadUInt16();
			byte[] compressed = reader.ReadBytes(compressedLength);

			// Record the incoming packet so the commit packet won't handle its
			// command until after we're done processing.
			unhandledPacketCount++;

			// Run the decompression asynchronously to avoid blocking the main
			// thread!
			pending_packets.Enqueue((compressed, whoAmI));
			StartDecompressWorker();
		}

		private static void StartDecompressWorker()
		{
			while (true)
			{
				int current = activeWorkers;
				if (current >= max_workers)
					break;

				if (Interlocked.CompareExchange(ref activeWorkers, current + 1, current) == current)
					Task.Run(ProcessDecompressQueueAsync);
			}
		}

		private static async Task ProcessDecompressQueueAsync()
		{
			try
			{
				while (true)
				{
					if (!pending_packets.TryDequeue(out var item))
					{
						await Task.Delay(10);

						if (pending_packets.IsEmpty)
							break;

						continue;
					}

					DecompressAndApply(item.compressed);
				}

				while (pending_packets.TryDequeue(out var item))
					DecompressAndApply(item.compressed);
			}
			finally
			{
				Interlocked.Exchange(ref activeWorkers, 0);

				if (!pending_packets.IsEmpty)
					StartDecompressWorker();
			}
		}

		private static void DecompressAndApply(byte[] compressed)
		{
			using var ms = new MemoryStream(compressed);
			using var input = new DeflateStream(ms, CompressionMode.Decompress);
			using var r = new BinaryReader(input);

			var mode = (DeltaMode)r.ReadByte();

			switch (mode)
			{
				case DeltaMode.Sparse:
					{
						int count = r.ReadUInt16();
						for (int i = 0; i < count; i++)
						{
							ushort x = r.ReadUInt16();
							ushort y = r.ReadUInt16();
							MapTile tile = ReadTile(r);
							UpdateTile(x, y, tile);
						}

						break;
					}

				case DeltaMode.Chunk:
					{
						ushort x = (ushort)(r.ReadByte() * chunk_width);
						ushort y = (ushort)(r.ReadByte() * chunk_height);

						for (int dy = 0; dy < chunk_height; dy++)
							for (int dx = 0; dx < chunk_width; dx++)
							{
								ushort tx = (ushort)(x + dx);
								ushort ty = (ushort)(y + dy);
								MapTile tile = ReadTile(r);
								UpdateTile(tx, ty, tile);
							}

						break;
					}
			}

			// Now that we're done, we can decrement.
			Interlocked.Decrement(ref unhandledPacketCount);

			Debug.Assert(unhandledPacketCount >= 0);
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
				RecordedMap.SetTile(x, y, ref tile);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				Main.Map.SetTile(x, y, ref tile);
		}

		public override void OnSend(ModPacket packet)
		{
			using var ms = new MemoryStream();

			using (var output = new DeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
			using (var writer = new BinaryWriter(output))
			{
				writer.Write((byte)Mode);

				switch (Mode)
				{
					case DeltaMode.Sparse:
						{
							writer.Write((ushort)SparseEntries.Count);

							foreach (var entry in SparseEntries)
							{
								writer.Write(entry.X);
								writer.Write(entry.Y);
								WriteTile(writer, entry.Tile);
							}

							break;
						}

					case DeltaMode.Chunk:
						{
							writer.Write(ChunkX);
							writer.Write(ChunkY);

							for (int dy = 0; dy < chunk_height; dy++)
								for (int dx = 0; dx < chunk_width; dx++)
								{
									MapTile tile = GetChunkTile(ChunkData, dx, dy);
									WriteTile(writer, tile);
								}

							break;
						}
				}
			}

			byte[] compressed = ms.ToArray();
			packet.Write((ushort)compressed.Length);
			packet.Write(compressed);
		}

		private static void WriteTile(BinaryWriter w, MapTile tile)
		{
			w.Write(tile.Type);
			w.Write(tile.Light);
			w.Write(tile.Color);
		}

		// Potential improvements:
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
				var sparse = new List<SparseEntry>(capacity: chunk_area);
				var chunk = new MapTile[chunk_area];

				var chunkRect = new Rectangle(
					cx,
					cy,
					Math.Min(chunk_width, width - cx),
					Math.Min(chunk_height, height - cy)
				);

				bool changed = false;
				int diffCount = 0;

				for (int dy = 0; dy < chunkRect.Height; dy++)
				for (int dx = 0; dx < chunkRect.Width; dx++)
				{
					ushort tx = (ushort)(chunkRect.X + dx);
					ushort ty = (ushort)(chunkRect.Y + dy);
					MapTile currentTile = map[tx, ty];
					MapTile? compareTile = comparisonMap?[tx, ty];

					if (!compareTile.HasValue || currentTile.Light > compareTile?.Light)
					{
						changed = true;
						diffCount++;
						sparse.Add(new SparseEntry(tx, ty, currentTile));
						GetChunkTile(chunk, dx, dy) = currentTile;
					}
				}

				// If there were not changes then discard and move on to
				// the next chunk.
				if (!changed)
					continue;

				// Now we actually decide how the delta will be represented.
				// TODO: Look into the values and tweak for efficiency?

				// Sparse size:
				// 1 + 2 + (2 + 2 + 2 + 1 + 1)n

				// Chunk
				// 1 + 2 + (2 + 1 + 1)(200 * 150)

				// These values meet at 15000 differences.

				if (diffCount <= 15000)
				{
					var packetData = new SyncMapData
					{
						Mode = DeltaMode.Sparse,
						ChunkX = 0,
						ChunkY = 0,
						SparseEntries = sparse
					};
					packets.Add(packetData);

				}
				else
				{
					var packetData = new SyncMapData
					{
						Mode = DeltaMode.Chunk,
						ChunkX = (byte)(cx / chunk_width),
						ChunkY = (byte)(cy / chunk_height),
						ChunkData = chunk
					};
					packets.Add(packetData);
				}
			}

			if (packets.Count == 0)
				return;

			packets.Add(new CommitMapData());
		}

		public static async Task SendQueuedDataAsync(int toClient = -1)
		{
			Main.NewText($"Send request: {packets.Count} packets queued!", Color.LightGreen);

			if (packets.Count == 0)
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					Main.NewText(Language.GetTextValue("Mods.SpiritReforged.Misc.UnchangedMap"), new Color(255, 240, 20));
					MapUpdated = false;
				}

				return;
			}

			Main.NewText("Sending map data...", Color.LightGreen);

			foreach (var packetData in packets)
			{
				packetData.Send(toClient: toClient);
			}

			Main.NewText("Map data sent.", Color.LightGreen);

			packets.Clear();
		}
	}

	/// <summary> Marks the end of a stream of map syncing packets and commits the data. </summary>
	internal sealed class CommitMapData : PacketData
	{
		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			// We can't execute immediately since packets may not be finished
			// processing.
			Task.Run(async () =>
			{
				// Wait until we hit zero packets.
				while (unhandledPacketCount > 0)
					await Task.Delay(100);

				// Actual logic should be handled on the main thread so we'll
				// mark the commit as needing to be handled.
				handleCommit = whoAmI;
			});
		}

		public override void OnSend(ModPacket modPacket) { }
	}

	/// <summary> Simply notifies everybody of updated map data on the server. </summary>
	internal sealed class NotifyMapData : PacketData
	{
		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			// We can't execute immediately since packets may not be finished
			// processing.
			Task.Run(async () =>
			{
				// Wait until we hit zero packets.
				while (unhandledPacketCount > 0)
					await Task.Delay(100);

				if (Main.netMode == NetmodeID.Server)
				{
					new NotifyMapData().Send();
					return;
				}

				MapUpdated = true;
			});
		}

		public override void OnSend(ModPacket modPacket) { }
	}
}