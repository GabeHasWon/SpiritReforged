using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.NPCCommon;

public class SummontTagGlobalNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	/// <summary> The common duration for summon tag bonuses. </summary>
	public const int Duration = 240;

	private int _summonTag;
	private int _duration;

	/// <summary> Applies summon tag to an NPC. Use <see cref="NPCHelper.ApplySummonTag"/> instead. </summary>
	public void ApplySummonTag(int damage, int npcIndex, bool sync = true)
	{
		_summonTag = damage;
		_duration = Duration;

		if (sync && Main.netMode != NetmodeID.SinglePlayer)
			new SummonTagData((byte)npcIndex, (byte)damage).Send();
	}

	public override void ResetEffects(NPC npc)
	{
		if (--_duration <= 0)
			_summonTag = 0;
	}

	public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (projectile.IsMinionOrSentryRelated)
			modifiers.FinalDamage.Flat += _summonTag;
	}
}

internal class SummonTagData : PacketData
{
	private readonly byte _index;
	private readonly byte _damage;

	public SummonTagData() { }
	public SummonTagData(byte index, byte damage)
	{
		_index = index;
		_damage = damage;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		byte npc = reader.ReadByte();
		byte damage = reader.ReadByte();

		if (Main.netMode == NetmodeID.Server)
			new SummonTagData(npc, damage).Send(ignoreClient: whoAmI);

		Main.npc[npc].ApplySummonTag(damage, false);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_index);
		modPacket.Write(_damage);
	}
}
