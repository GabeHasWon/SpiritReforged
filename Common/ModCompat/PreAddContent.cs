using MonoMod.RuntimeDetour;
using System.Reflection;

namespace SpiritReforged.Common.ModCompat;

/// <summary> Selectively disables content from other mods using <see cref="string"/> identifiers. </summary>
internal class PreAddContent : ILoadable
{
	internal delegate bool orig_AddContent(Mod self, ILoadable instance);
	private static Hook PreAddContentHook = null;

	/// <summary> The full names of content to disable. Must be formatted as "Content, Mod". </summary>
	private static readonly Dictionary<string, string> NameToMod = new() { { "FloatingItemWorld", "SpiritMod" }, { "HeroMemorialMicropass", "SpiritMod" },
		{ "StargrassMicropass", "SpiritMod" }, { "ZombieGlobalNPC", "SpiritMod" }, { "BoidHost", "SpiritMod" }, { "OceanGlobalTile", "SpiritMod" },
		{ "CloudRandomUpdate", "SpiritMod" }};

	/// <summary> Hooks <see cref="Mod.AddContent"/> to control whether content from other mods can be added to the game.<br/>
	/// Must be called in the mod's constructor to ignore mod load order. </summary>
	internal static void AddContentHook(Mod mod)
	{
		MethodInfo info = mod.GetType().GetMethod("AddContent", BindingFlags.Instance | BindingFlags.Public, [typeof(ILoadable)]);
		PreAddContentHook = new Hook(info, HookAddContent, true);
	}

	private static bool HookAddContent(orig_AddContent orig, Mod self, ILoadable instance)
	{
		foreach (string str in NameToMod.Keys)
		{
			string modName = NameToMod[str];
			string contentName = str;

			if (self.Name == modName && instance.GetType().Name == contentName)
			{
				SpiritReforgedMod.Instance.Logger.Info("Disabled " + modName + $".{contentName}");
				NameToMod.Remove(str); //Consume the entry

				return false;
			}
		}

		return orig(self, instance);
	}

	public void Load(Mod mod) { } //This is useless because it is called too late
	public void Unload()
	{
		PreAddContentHook?.Undo();
		PreAddContentHook = null;
	}
}
