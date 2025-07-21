namespace SpiritReforged.Common.NPCCommon;

/// <summary> Detours <see cref="NPC.ExcludedFromDeathTally"/> and <see cref="NPC.IsNPCValidForBestiaryKillCredit"/>. </summary>
internal interface IDeathCount
{
	/// <summary> Whether this NPC's death should be recorded for banner drop and bestiary purposes. </summary>
	public bool TallyDeath(NPC npc);
}

internal sealed class DeathCountDetour : ILoadable
{
	public void Load(Mod mod)
	{
		On_NPC.ExcludedFromDeathTally += IsExcluded;
		On_NPC.IsNPCValidForBestiaryKillCredit += IsIncludedBestiary;
	}

	private static bool IsExcluded(On_NPC.orig_ExcludedFromDeathTally orig, NPC self)
	{
		if (self.ModNPC is IDeathCount c && !c.TallyDeath(self))
			return true; //Skips orig

		return orig(self);
	}

	private static bool IsIncludedBestiary(On_NPC.orig_IsNPCValidForBestiaryKillCredit orig, NPC self)
	{
		if (self.ModNPC is IDeathCount c && !c.TallyDeath(self))
			return false; //Skips orig

		return orig(self);
	}

	public void Unload() { }
}