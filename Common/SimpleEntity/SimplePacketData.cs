using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.SimpleEntity;

internal class SpawnSimpleEntityData : PacketData
{
	private readonly short _type;
	private readonly Vector2 _position;

	public SpawnSimpleEntityData() { }
	public SpawnSimpleEntityData(short type, Vector2 position)
	{
		_type = type;
		_position = position;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short entityType = reader.ReadInt16();
		Vector2 position = reader.ReadVector2();

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new SpawnSimpleEntityData(entityType, position).Send(ignoreClient: whoAmI);

		SimpleEntitySystem.NewEntity(entityType, position, true);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_type);
		modPacket.WriteVector2(_position);
	}
}

internal class KillSimpleEntityData : PacketData
{
	private readonly short _index;

	public KillSimpleEntityData() { }
	public KillSimpleEntityData(short index) => _index = index;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short index = reader.ReadInt16();

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new KillSimpleEntityData(index).Send(ignoreClient: whoAmI);

		SimpleEntitySystem.Entities[index].Kill();
	}

	public override void OnSend(ModPacket modPacket) => modPacket.Write(_index);
}
