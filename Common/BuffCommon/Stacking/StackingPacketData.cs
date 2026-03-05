using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.BuffCommon.Stacking;

/// <summary> Syncs <see cref="StackingBuff"/> data when applied. </summary>
internal class StackAddData : PacketData
{
	private readonly string _name;
	private readonly short _npc;
	private readonly short _time;
	private readonly byte _stack;

	public StackAddData() { }
	public StackAddData(string name, short npc, short time, byte stack)
	{
		_name = name;
		_npc = npc;
		_time = time;
		_stack = stack;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		string name = reader.ReadString();
		short npcIndex = reader.ReadInt16();
		short time = reader.ReadInt16();
		byte stack = reader.ReadByte();

		if (Main.netMode == NetmodeID.Server)
			new StackAddData(name, npcIndex, time, stack).Send(ignoreClient: whoAmI); //Relay to other clients

		if (npcIndex > 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].TryGetGlobalNPC(out StackingNPC gNPC))
			gNPC.AddBuff(name, time, stack);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_name);
		modPacket.Write(_npc);
		modPacket.Write(_time);
		modPacket.Write(_stack);
	}
}

/// <summary> Syncs <see cref="StackingBuff"/> data when removed. </summary>
internal class StackRemovalData : PacketData
{
	private readonly string _name;
	private readonly short _npc;

	public StackRemovalData() { }
	public StackRemovalData(string name, short npc)
	{
		_name = name;
		_npc = npc;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		string name = reader.ReadString();
		short npcIndex = reader.ReadInt16();

		if (Main.netMode == NetmodeID.Server)
			new StackRemovalData(name, npcIndex).Send(ignoreClient: whoAmI); //Relay to other clients

		if (npcIndex > 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].TryGetGlobalNPC(out StackingNPC gNPC))
			gNPC.RemoveBuff(name);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_name);
		modPacket.Write(_npc);
	}
}