namespace SpiritReforged.Common.NPCCommon;

public static class NPCHelper
{
	/// <summary> <see cref="NPC.CanBeChasedBy"/> without checking <see cref="NPC.chaseable"/>. </summary>
	public static bool CanBeStruck(this NPC npc, bool allowDummy = true)
	{
		if (allowDummy && npc.type == NPCID.TargetDummy)
			return true;

		return npc.active && npc.lifeMax > 5 && !npc.dontTakeDamage && !npc.friendly && !npc.immortal;
	}

	/// <summary> Applies summon tag of <paramref name="damage"/> to this NPC. </summary>
	public static void ApplySummonTag(this NPC npc, int damage, bool sync = true)
	{
		if (npc.TryGetGlobalNPC(out SummontTagGlobalNPC tagNPC))
			tagNPC.ApplySummonTag(damage, npc.whoAmI, sync);
	}

	public static Color DrawColor(this NPC npc, Color drawColor) => npc.GetAlpha(npc.GetNPCColorTintedByBuffs(drawColor));

	public static void BuffImmune(int type, bool whipsToo = false)
	{
		if (whipsToo)
			NPCID.Sets.ImmuneToAllBuffs[type] = true;
		else
			NPCID.Sets.ImmuneToRegularBuffs[type] = true;
	}

	public static void BuffImmune(ModNPC npc, bool whipsToo = false) => BuffImmune(npc.Type, whipsToo);

	public static void ImmuneTo(ModNPC npc, params int[] buffs)
	{
		foreach (int buff in buffs)
			NPCID.Sets.SpecificDebuffImmunity[npc.Type][buff] = true;
	}

	public static void ImmuneTo<T>(ModNPC npc, params int[] buffs) where T : ModBuff => ImmuneTo(npc, [.. new List<int>(buffs) { ModContent.BuffType<T>() }]);

	public static void ImmuneTo<T1, T2>(ModNPC npc, params int[] buffs) where T1 : ModBuff where T2 : ModBuff 
		=> ImmuneTo(npc, [.. new List<int>(buffs) { ModContent.BuffType<T1>(), ModContent.BuffType<T2>() }]);

	public static void ImmuneTo<T1, T2, T3>(ModNPC npc, params int[] buffs) where T1 : ModBuff where T2 : ModBuff where T3 : ModBuff
		=> ImmuneTo(npc, [.. new List<int>(buffs) { ModContent.BuffType<T1>(), ModContent.BuffType<T2>(), ModContent.BuffType<T3>() }]);
}
