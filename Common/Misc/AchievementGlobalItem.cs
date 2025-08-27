using System.Runtime.CompilerServices;
using Terraria.Achievements;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Misc;

internal sealed class AchievementModifications : GlobalItem
{
	// God forbid this game is normal. Because of how achievements are programmed, namely a lot of delegates, and how mod loading works,
	// I didn't want to directly modify the achivements (since they're saved as json and/or into achievements.dat at some point) which
	// could cause issues I don't want to debug - the achievements also make use of delegates often enough to where I'd be concerned
	// about variable capturing making these edits inconsistent/hard to debug. I didn't want to expand ItemID.Sets.Workbenches because
	// we're literally 1 day off of the Named ID Sets coming into tMod and that'd be much nicer.
	// Instead, I did this hacky workaround.
	// It works, so too bad.

	public override bool OnPickup(Item item, Player player)
	{
		if (SpiritSets.Timber[item.type])
			CompleteAchievement(Main.Achievements.GetAchievement("TIMBER"));

		return true;
	}

	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (context is RecipeItemCreationContext && SpiritSets.Workbench[item.type])
			CompleteAchievement(Main.Achievements.GetAchievement("BENCHED"));
	}

	private static void CompleteAchievement(Achievement achievement)
	{
		if (achievement.IsCompleted)
			return;

		ref int completedCount = ref GetCount(achievement);
		completedCount = GetConditions(achievement).Count - 1;

		CallComplete(achievement, null);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_completedCount")]
	static extern ref int GetCount(Achievement achievement);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_conditions")]
	static extern ref Dictionary<string, AchievementCondition> GetConditions(Achievement achievement);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "OnConditionComplete")]
	static extern void CallComplete(Achievement achievement, AchievementCondition cond);
}