using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.NPCCommon;

/// <summary>
/// Syncs an NPC's velocity from a client. If you're on a server, use <see cref="NPC.netUpdate"/> instead.
/// </summary>
internal class NPCVelocityPacketData : PacketData
{
	private readonly short _who;
	private readonly Vector2 _velocity;

	public NPCVelocityPacketData() { }

	public NPCVelocityPacketData(short who, Vector2 velocity)
	{
		_who = who;
		_velocity = velocity;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short who = reader.ReadInt16();
		Vector2 vel = reader.ReadVector2();

		if (Main.netMode == NetmodeID.Server)
			new NPCVelocityPacketData(who, vel).Send(ignoreClient: whoAmI);

		Main.npc[who].velocity = vel;
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_who);
		modPacket.WriteVector2(_velocity);
	}
}