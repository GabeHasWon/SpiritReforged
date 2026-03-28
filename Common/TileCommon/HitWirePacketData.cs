using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.TileCommon;

/// <summary>
/// Allows <see cref="TileLoader.HitWire(int, int, int)"/> to be called from a client to run on the server. Unused atm
/// </summary>
internal class HitWirePacketData : PacketData
{
	private readonly int _x;
	private readonly int _y;

	public HitWirePacketData()
	{
	}

	public HitWirePacketData(int x, int y)
	{
		_x = x;
		_y = y;
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write((short)_x);
		modPacket.Write((short)_y);
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		int x = reader.ReadInt16();
		int y = reader.ReadInt16();
		Tile tile = Main.tile[x, y];

		TileLoader.HitWire(x, y, tile.TileType);

		if (Main.netMode == NetmodeID.Server)
			Send(-1, whoAmI);
	}
}
