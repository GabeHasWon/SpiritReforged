using System.Runtime.CompilerServices;
using Terraria.Achievements;

namespace SpiritReforged.Common.Misc;

internal sealed class AchievementModifications : GlobalItem
{
	/// <summary> Adds the provided tile type to <see cref="ItemID.Sets.Workbenches"/>. </summary>
	public static void ConfirmWorkBench(short type)
	{
		short[] array = ItemID.Sets.Workbenches;

		Array.Resize(ref array, array.Length + 1);
		array[^1] = type;
	}

	public override bool OnPickup(Item item, Player player)
	{
		if (SpiritSets.Timber[item.type])
			CompleteAchievement(Main.Achievements.GetAchievement("TIMBER"));

		return true;
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