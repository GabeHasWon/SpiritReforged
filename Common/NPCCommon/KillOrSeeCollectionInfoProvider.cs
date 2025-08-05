using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Common.NPCCommon;

internal class KillOrSeeCollectionInfoProvider(string persistentId) : IBestiaryUICollectionInfoProvider
{
	private readonly string _persistentIdentifierToCheck = persistentId;

	public BestiaryUICollectionInfo GetEntryUICollectionInfo()
	{
		bool wasSeenAlready = Main.BestiaryTracker.Sights.GetWasNearbyBefore(_persistentIdentifierToCheck);
		BestiaryEntryUnlockState unlockStateByKillCount = GetUnlockStateByKillCount(Main.BestiaryTracker.Kills.GetKillCount(_persistentIdentifierToCheck), wasSeenAlready);
		BestiaryUICollectionInfo result = default;
		result.UnlockState = unlockStateByKillCount;
		return result;
	}

	public static BestiaryEntryUnlockState GetUnlockStateByKillCount(int killCount, bool wasSeenAlready)
	{
		if (wasSeenAlready || killCount > 0)
			return BestiaryEntryUnlockState.CanShowDropsWithDropRates_4;

		return BestiaryEntryUnlockState.NotKnownAtAll_0;
	}
}