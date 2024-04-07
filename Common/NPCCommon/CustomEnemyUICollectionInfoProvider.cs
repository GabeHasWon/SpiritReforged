﻿using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Common.NPCCommon;

internal class CustomEnemyUICollectionInfoProvider(string persistentId, bool quickUnlock, float maxKills = 50) : IBestiaryUICollectionInfoProvider
{
	private readonly string _persistentIdentifierToCheck = persistentId;
	private readonly bool _quickUnlock = quickUnlock;
	private readonly float _maxKills = maxKills;

	public BestiaryUICollectionInfo GetEntryUICollectionInfo()
	{
		BestiaryEntryUnlockState unlockStateByKillCount = GetUnlockStateByKillCount(Main.BestiaryTracker.Kills.GetKillCount(_persistentIdentifierToCheck), _quickUnlock, _maxKills);
		BestiaryUICollectionInfo result = default;
		result.UnlockState = unlockStateByKillCount;
		return result;
	}

	public static BestiaryEntryUnlockState GetUnlockStateByKillCount(int killCount, bool quickUnlock, float maxKills)
	{
		if (quickUnlock && killCount > 0 || killCount >= maxKills)
			return BestiaryEntryUnlockState.CanShowDropsWithDropRates_4;
		else if (killCount >= maxKills / 2)
			return BestiaryEntryUnlockState.CanShowDropsWithoutDropRates_3;
		else if (killCount >= maxKills / 5)
			return BestiaryEntryUnlockState.CanShowStats_2;
		else if (killCount >= maxKills / 50)
			return BestiaryEntryUnlockState.CanShowPortraitOnly_1;

		return BestiaryEntryUnlockState.NotKnownAtAll_0;
	}
}