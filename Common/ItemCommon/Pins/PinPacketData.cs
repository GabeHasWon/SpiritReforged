using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.ItemCommon.Pins;

internal class AddPinData : PacketData
{
	private readonly Vector2 _position;
	private readonly string _name;

	public AddPinData() { }
	public AddPinData(Vector2 position, string name)
	{
		_position = position;
		_name = name;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		Vector2 cursorPos = reader.ReadVector2();
		string pinName = reader.ReadString();

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new AddPinData(cursorPos, pinName).Send();

		ModContent.GetInstance<PinSystem>().SetPin(pinName, cursorPos);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.WriteVector2(_position);
		modPacket.Write(_name);
	}
}

internal class RemovePinData : PacketData
{
	private readonly string _name;

	public RemovePinData() { }
	public RemovePinData(string name) => _name = name;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		string removePinName = reader.ReadString();

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new RemovePinData(removePinName).Send();

		ModContent.GetInstance<PinSystem>().RemovePin(removePinName);
	}

	public override void OnSend(ModPacket modPacket) => modPacket.Write(_name);
}