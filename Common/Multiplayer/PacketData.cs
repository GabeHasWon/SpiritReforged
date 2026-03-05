using System.IO;
using System.Linq;

namespace SpiritReforged.Common.Multiplayer;

/// <summary> Encapsulates custom multiplayer data to send interchangeably between server and clients.<br/>
/// Must include a parameterless constructor for initialization purposes. </summary>
internal abstract class PacketData
{
	/// <summary>
	/// Whether this packet will be logged. True by default.
	/// </summary>
	public virtual bool Log => true;

	/// <summary> This must be called after creating a new packet instance in order for it to be sent. </summary>
	public void Send(int toClient = -1, int ignoreClient = -1)
	{
		byte id = MultiplayerHandler.PacketTypes.Where(x => x.Value.GetType() == GetType()).First().Key;

		var packet = SpiritReforgedMod.Instance.GetPacket();
		packet.Write(id);
		OnSend(packet);
		packet.Send(toClient, ignoreClient);
	}

	/// <summary> Use <paramref name="modPacket"/> to write data here (usually fields assigned by the constructor).<br/>
	/// Sending is automatic and should not be done here. </summary>
	public abstract void OnSend(ModPacket modPacket);
	/// <summary> Read your packet data here. <br/> Remember that only <paramref name="reader"/> should be used to get variable data, not fields. </summary>
	/// <param name="whoAmI">The index of player this message is from. Only relevant for server code.</param>
	public abstract void OnReceive(BinaryReader reader, int whoAmI);
}
