using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.Map;

namespace SpiritReforged.Content.Forest.Cartography;

public class MappingSystem : ModSystem
{
	public static int Buffer { get; private set; }
	public static int BufferMax => Main.maxTilesX * Main.maxTilesY / 12000;

	internal static WorldMap RecordedMap;

	public static void SetBuffer() => Buffer = BufferMax;

	public override void ClearWorld() => RecordedMap = null;

	public override void PreUpdatePlayers()
	{
		if (Buffer > 0 && Main.netMode != NetmodeID.SinglePlayer)
		{
			if (Buffer == 1)
			{
				Main.NewText("Finished sync (DEBUG)");
				Main.refreshMap = true;
			}

			new SyncMapData(RecordedMap).Send();
			Buffer--;
		}
	}

	public static void SetMap()
	{
		if (Buffer > 0 || Main.netMode == NetmodeID.SinglePlayer)
			return;

		RecordedMap ??= Main.Map;

		for (int x = 0; x < RecordedMap.MaxWidth; x++)
		{
			for (int y = 0; y < RecordedMap.MaxHeight; y++)
			{
				var tileToSend = Main.Map[x, y];

				if (tileToSend.Light > RecordedMap[x, y].Light)
					RecordedMap.SetTile(x, y, ref tileToSend);
			}
		}

		Buffer = BufferMax;
		//new StartSyncMapData().Send();
	}
}

internal class SyncMapData : PacketData
{
	private readonly WorldMap _map;

	public SyncMapData() { }
	public SyncMapData(WorldMap map) => _map = map;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		MappingSystem.RecordedMap ??= new(Main.maxTilesX, Main.maxTilesY);

		ScanOverMap(MappingSystem.RecordedMap, (x, y) =>
		{
			byte light = reader.ReadByte();

			var tileToPost = MapHelper.CreateMapTile(x, y, light);
			MappingSystem.RecordedMap.SetTile(x, y, ref tileToPost);
		});

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new SyncMapData(MappingSystem.RecordedMap).Send(ignoreClient: whoAmI);
	}

	public override void OnSend(ModPacket modPacket) => ScanOverMap(_map, (x, y) => WriteSingleMapTile(modPacket, _map[x, y]));

	private static void ScanOverMap(WorldMap map, Action<int, int> action)
	{
		int maxTiles = (map.MaxWidth - 1) * (map.MaxHeight - 1);

		for (int t = Segment(); t < Segment(-1); t++)
		{
			int x = t / map.MaxHeight;
			int y = t % map.MaxHeight;

			action.Invoke(x, y);
		}

		int Segment(int move = 0) => (int)(maxTiles * (1f - (float)(MappingSystem.Buffer - move) / MappingSystem.BufferMax));
	}

	private static void WriteSingleMapTile(ModPacket modPacket, MapTile tile) => modPacket.Write(tile.Light); //Only write light levels because we can infer the rest
}

internal class StartSyncMapData : PacketData
{
	public StartSyncMapData() { }

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		MappingSystem.SetBuffer();
	}

	public override void OnSend(ModPacket modPacket)
	{
		MappingSystem.SetBuffer();
	}
}