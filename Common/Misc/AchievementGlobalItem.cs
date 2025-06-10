using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Ocean.Tiles.Furniture;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Savanna.Tiles.Furniture;
using System.Runtime.CompilerServices;
using Terraria.Achievements;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Misc;

internal class AchievementModifications : GlobalItem
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
		// Add the TIMBER achievement when picking up Drywood
		if (item.type == AutoContent.ItemType<Drywood>())
			CompleteAchievement(Main.Achievements.GetAchievement("TIMBER"));

		return true;
	}

	public override void OnCreated(Item item, ItemCreationContext context)
	{
		// Add the BENCHED achievement to crafting any Reforged workbench
		if (context is RecipeItemCreationContext && (item.type == TileToItem<DriftwoodWorkBench>() || item.type == TileToItem<DrywoodWorkBench>()))
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

	private static int TileToItem<T>() where T : ModTile, IAutoloadTileItem => ModContent.GetInstance<T>().AutoItem().type;

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_completedCount")]
	static extern ref int GetCount(Achievement achievement);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_conditions")]
	static extern ref Dictionary<string, AchievementCondition> GetConditions(Achievement achievement);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "OnConditionComplete")]
	static extern void CallComplete(Achievement achievement, AchievementCondition cond);
}
