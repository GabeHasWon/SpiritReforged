using Terraria.Audio;

namespace SpiritReforged.Content.Snow.Frostbite;

internal class WindSoundPlayer : ModSystem
{
	private const int soundDistance = 300;

	public static readonly SoundStyle Blizzard = new("SpiritReforged/Assets/SFX/Ambient/Blizzard_Loop")
	{
		Volume = .5f,
		SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
		PlayOnlyIfFocused = true,
		IsLooped = true
	};

	private static byte timeOut;

	public override void PostUpdatePlayers()
	{
		if (--timeOut == 0)
			SoundEngine.FindActiveSound(in Blizzard)?.Stop();
		else
			SoundEngine.FindActiveSound(in Blizzard)?.Resume();
	}

	public static void StartSound(Vector2 origin)
	{
		timeOut = 5;

		SoundEngine.PlaySound(Blizzard, origin);
		var activeSound = SoundEngine.FindActiveSound(in Blizzard);

		if (activeSound != null && activeSound.Position.HasValue)
		{
			var player = Main.LocalPlayer;
			float volume = Math.Clamp(1f - player.Distance(activeSound.Position.Value) / soundDistance, 0, 1);

			if (player.Distance(activeSound.Position.Value) > player.Distance(origin))
				activeSound.Position = origin; //Move the sound to the closest origin

			activeSound.Volume = volume; //Adjust volume based on distance
		}
	}
}
