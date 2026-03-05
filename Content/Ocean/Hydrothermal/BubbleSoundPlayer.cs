using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Hydrothermal;

internal class BubbleSoundPlayer : ModSystem
{
	private static readonly SoundStyle sound = new("SpiritReforged/Assets/SFX/Ambient/Bubbling") { SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, PlayOnlyIfFocused = true, IsLooped = true };
	private static bool stopped;

	public override void PostUpdatePlayers()
	{
		var player = Main.LocalPlayer;
		if (player.wet && Collision.WetCollision(player.position, player.width, player.height)) //Ambient sound logic
		{
			if (stopped)
			{
				SoundEngine.FindActiveSound(in sound)?.Resume();
				stopped = false;
			}
		}
		else
		{
			SoundEngine.FindActiveSound(in sound)?.Stop(); //Stop the sound if the player isn't submerged
			stopped = true;
		}
	}

	public static void StartSound(Vector2 origin)
	{
		const int soundDistance = 250;

		if (!stopped)
		{
			SoundEngine.PlaySound(sound, origin);
			var activeSound = SoundEngine.FindActiveSound(in sound);

			if (activeSound != null && activeSound.Position.HasValue)
			{
				var player = Main.LocalPlayer;
				float volume = Math.Clamp(1f - player.Distance(activeSound.Position.Value) / soundDistance, 0, 1);

				if (player.Distance(activeSound.Position.Value) > player.Distance(origin))
					activeSound.Position = origin; //Move the sound to the closest vent

				activeSound.Volume = volume; //Adjust volume based on distance
			}
		}
	}
}
