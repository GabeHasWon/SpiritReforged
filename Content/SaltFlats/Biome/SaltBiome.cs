using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.GameContent.Personalities;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBiome : ModBiome
{
	private int GetMusic()
	{
		if (Main.LocalPlayer.ZoneGraveyard || Main.bloodMoon)
			return -1;

		string name = "Salt"; //SpiritReforgedMod.SwapMusic ? "SaltOtherworld" : "Salt";
		return Main.dayTime ? MusicLoader.GetMusicSlot(Mod, $"Assets/Music/{name}") : MusicLoader.GetMusicSlot(Mod, $"Assets/Music/{name}Night");
	}

	public override void SetStaticDefaults()
	{
		NPCHappinessHelper.SetAverage<SaltBiome>(ModContent.GetInstance<SnowBiome>(), ModContent.GetInstance<DesertBiome>());
		SceneTileCounter.SurveyByType.Add(Type, new([ModContent.TileType<SaltBlockReflective>(), ModContent.TileType<SaltBlockDull>()], 200));
	}

	public override ModWaterStyle WaterStyle => ModContent.GetInstance<SaltWaterStyle>();
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeMedium;
	public override int Music => GetMusic();

	public override bool IsBiomeActive(Player player)
	{
		bool surface = player.ZoneSkyHeight || player.ZoneOverworldHeight;
		return SceneTileCounter.SurveyByType[Type].Success && surface;
	}
}