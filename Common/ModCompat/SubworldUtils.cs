namespace SpiritReforged.Common.ModCompat;

internal static class SubworldUtils
{
	public static bool InSubworld() => CrossMod.SubworldLibrary.Enabled && ((Mod)CrossMod.SubworldLibrary).Call("Current") is not null;
}
