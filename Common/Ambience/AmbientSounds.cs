using ReLogic.Utilities;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Content.Ocean;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.Audio;
using Terraria.GameContent.Events;

namespace SpiritReforged.Common.Ambience;

/// <summary> Handles ambient sound loops, starting, maintaining and stopping them. </summary>
[Autoload(Side = ModSide.Client)]
internal class AmbientSounds : ModSystem
{
	private const string Path = "SpiritReforged/Assets/SFX/Ambient/";

	public static readonly SoundStyle NighttimeAmbience = new(Path + nameof(NighttimeAmbience), SoundType.Ambient) { IsLooped = true };
	public static readonly SoundStyle DesertWind = new(Path + nameof(DesertWind), SoundType.Ambient) { IsLooped = true };
	public static readonly SoundStyle CaveAmbience = new(Path + nameof(CaveAmbience), SoundType.Ambient) { IsLooped = true };
	public static readonly SoundStyle UnderwaterAmbience = new(Path + nameof(UnderwaterAmbience), SoundType.Ambient) { IsLooped = true };
	public static readonly SoundStyle ZigguratAmbience = new(Path + nameof(ZigguratAmbience), SoundType.Ambient) { IsLooped = true };
	public static readonly SoundStyle SavannaDayAmbience = new(Path + nameof(SavannaDayAmbience), SoundType.Ambient) { IsLooped = true };
	public static readonly SoundStyle SavannaNightAmbience = new(Path + nameof(SavannaNightAmbience), SoundType.Ambient) { IsLooped = true };

	public static event Action OnUpdateAmbience;

	private static readonly Dictionary<string, SlotId> SoundSlots = [];

	public override void PostUpdatePlayers()
	{
		OnUpdateAmbience?.Invoke();

		if (Main.dedServ || !ModContent.GetInstance<ReforgedClientConfig>().AmbientSounds)
			return;

		var player = Main.LocalPlayer;

		bool inSavanna = !player.ZoneDesert && !player.InModBiome<ZigguratBiome>() && player.InModBiome<SavannaBiome>();
		bool savannaDay = inSavanna && player.ZoneOverworldHeight && Main.dayTime;
		UpdateSingleSound(SavannaDayAmbience, 0.002f, savannaDay, 0.9f);

		bool savannaNight = inSavanna && player.ZoneOverworldHeight && !Main.dayTime;
		UpdateSingleSound(SavannaNightAmbience, 0.002f, savannaNight, 0.9f);

		bool ziggurat = player.InModBiome<ZigguratBiome>();
		UpdateSingleSound(ZigguratAmbience, 0.002f, ziggurat, 0.6f);

		bool nightTimeCondition = player.ZonePurity && player.ZoneOverworldHeight && !Main.dayTime && !savannaNight;
		UpdateSingleSound(NighttimeAmbience, 0.005f, nightTimeCondition);

		bool desertWind = player.ZoneDesert && player.ZoneOverworldHeight && !Sandstorm.Happening && !Main.raining && !player.ZoneBeach && !ziggurat;
		UpdateSingleSound(DesertWind, 0.005f, desertWind);

		bool caveAmbience = player.ZoneRockLayerHeight;
		UpdateSingleSound(CaveAmbience, 0.0005f, caveAmbience);

		bool submerged = player.GetModPlayer<OceanPlayer>().Submerged(10);
		UpdateSingleSound(UnderwaterAmbience, 0.08f, submerged);
	}

	private static void UpdateSingleSound(SoundStyle style, float lerpFactor, bool condition, float maxVolume = 1)
	{
		const float cutoff = 0.03f;
		string key = style.SoundPath;

		if (condition)
		{
			if (!SoundSlots.ContainsKey(key))
				SoundSlots.Add(key, SoundEngine.PlaySound(style with { Volume = 0.05f }));

			if (SoundEngine.TryGetActiveSound(SoundSlots[key], out ActiveSound sound))
				sound.Volume = MathHelper.Lerp(sound.Volume, maxVolume, lerpFactor);
			else
				SoundSlots[key] = SoundEngine.PlaySound(style with { Volume = 0.05f });
		}
		else if (SoundSlots.TryGetValue(key, out var slot) && SoundEngine.TryGetActiveSound(slot, out ActiveSound sound))
		{
			sound.Volume = MathHelper.Lerp(sound.Volume, 0, lerpFactor);

			if (sound.Volume < cutoff)
			{
				sound.Stop();
				SoundSlots.Remove(key);
			}
		}
	}
}