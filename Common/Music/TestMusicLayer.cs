namespace SpiritReforged.Common.Music;

internal class TestMusicLayer : IMusicLayer
{
	public int MusicSlot => MusicLoader.GetMusicSlot(SpiritReforgedMod.Instance, "Assets/Music/Boss1PitchLayer");
	public int MatchSlot => MusicID.Boss1;

	public bool CanPlay(ref float fadeIn) => Main.curMusic == MusicID.Boss1 && !Main.mouseLeft;
	public void Load(Mod mod) { }
	public void Unload() { }
}
