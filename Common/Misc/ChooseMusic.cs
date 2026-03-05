namespace SpiritReforged.Common.Misc;

/// <summary> Allows forced music by using <see cref="SetMusic"/>. </summary>
internal class ChooseMusic : ILoadable
{
	private const byte CooldownMax = 30;
	private static byte Cooldown;

	private static int Music = -1;

	/// <summary> Temporarily sets the music ID to play. </summary>
	public static void SetMusic(int id)
	{
		Music = id;
		Cooldown = CooldownMax;
	}

	public void Load(Mod mod) => On_Main.UpdateAudio_DecideOnNewMusic += SelectMusic;

	private static void SelectMusic(On_Main.orig_UpdateAudio_DecideOnNewMusic orig, Main self)
	{
		if ((Cooldown = (byte)Math.Max(Cooldown - 1, 0)) == 0)
			Music = -1;

		orig(self);

		if (Music > -1)
			Main.newMusic = Music;
	}

	public void Unload() { }
}
