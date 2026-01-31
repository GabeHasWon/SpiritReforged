using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat;

internal class TEMP_RemnantsGenerationFix : ModSystem
{
	private delegate void hook_ModifyWorldGenTasks(List<GenPass> passes, ref double weight);

	public override void Load() => MonoModHooks.Add(typeof(SystemLoader).GetMethod(nameof(SystemLoader.ModifyWorldGenTasks)), DetourModifyWorldGenTasks);

	private static void DetourModifyWorldGenTasks(hook_ModifyWorldGenTasks orig, List<GenPass> tasks, ref double weight)
	{
		orig(tasks, ref weight);

		if (!CrossMod.Remnants.Enabled || CrossMod.Remnants.Instance.Version != new Version(2, 0, 9))
			return;

		int index = tasks.FindIndex(x => x.Name == "[R] Enchanted Swords");

		if (index != -1)
			tasks.RemoveAt(index); // Disable doesn't work this late
	}
}
