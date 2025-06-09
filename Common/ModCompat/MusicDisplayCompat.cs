namespace SpiritReforged.Common.ModCompat;

internal class MusicDisplayCompat : ModSystem
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.MusicDisplay.Enabled;
	public override void PostSetupContent()
	{
		var display = CrossMod.MusicDisplay.Instance;
		LocalizedText modName = Language.GetText("Mods.SpiritReforged.MusicDisplay.ModName");

		void AddMusic(string name)
		{
			LocalizedText author = Language.GetText("Mods.SpiritReforged.MusicDisplay." + name + ".Author");
			LocalizedText displayName = Language.GetText("Mods.SpiritReforged.MusicDisplay." + name + ".DisplayName");
			display.Call("AddMusic", (short)MusicLoader.GetMusicSlot(SpiritReforgedMod.Instance, "Assets/Music/" + name), displayName, author, modName);
		}

		AddMusic("Duststorm");
		AddMusic("Savanna");
		AddMusic("SavannaNight");
		AddMusic("DeepOcean");
		AddMusic("SavannaOtherworld");
		AddMusic("SavannaOtherworldNight");
		AddMusic("DuststormOtherworld");
	}
}