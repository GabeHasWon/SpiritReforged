using SpiritReforged.Common.WorldGeneration.Micropasses;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.Generation;
using Terraria.ModLoader.Core;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal abstract class SpookyMicropass : Micropass
{
	public delegate void hook_ModifyGen(object self, List<GenPass> tasks, ref double totalWeight);

	public abstract string ModifyType { get; }
	public abstract string MatchPass { get; }

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;
	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => -1;

	public override void Load() => LoadType();

	protected Type LoadType()
	{
		MethodInfo modifyMethod = GetDetouredMethod(out Type result);
		MonoModHooks.Add(modifyMethod, HookGen);
		return result;
	}

	protected MethodInfo GetDetouredMethod(out Type resultType)
	{
		Type[] types = AssemblyManager.GetLoadableTypes(CrossMod.Spooky.Instance.Code);
		resultType = types.FirstOrDefault(x => x.Name == ModifyType);
		MethodInfo modifyMethod = resultType.GetMethod("ModifyWorldGenTasks");
		return modifyMethod;
	}

	private void HookGen(hook_ModifyGen orig, object self, List<GenPass> passes, ref double totalWeight)
	{
		orig(self, passes, ref totalWeight);

		int index = passes.FindIndex(x => x.Name == MatchPass) + 1;

		if (index > 0)
			passes.Insert(index, new PassLegacy(WorldGenName, Run));
	}
}
