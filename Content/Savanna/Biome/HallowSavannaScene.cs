namespace SpiritReforged.Content.Savanna.Biome;

internal class HallowSavannaScene : ModSceneEffect
{
	private int SelectMusic()
	{
		if (Main.LocalPlayer.townNPCs > 2f)
			return -1;

		if (Main.dayTime)
			return MusicID.TheHallow;
		else
			return MusicLoader.GetMusicSlot(ModLoader.GetMod("SpiritMusic"), "Sounds/Music/HallowNight");
	}

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
	public override int Music => SelectMusic();
	public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<HallowSavannaBGStyle>();

	public override bool IsSceneEffectActive(Player player) => player.InModBiome<SavannaBiome>() && player.ZoneHallow;
}
