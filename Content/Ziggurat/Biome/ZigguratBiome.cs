namespace SpiritReforged.Content.Ziggurat.Biome;

public class ZigguratBiome : ModBiome
{
	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Ziggurat");
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
	public override string MapBackground => "SpiritReforged/Assets/Textures/Backgrounds/ZigguratMapBG";

	public override bool IsBiomeActive(Player player)
	{
		if (Common.WorldGeneration.Microbiomes.Biomes.Ziggurat.ZigguratBiome.Instance?.Area.Contains(player.Center.ToTileCoordinates()) == true)
		{
			int wallType = Framing.GetTileSafely(player.Center).WallType;
			return wallType != WallID.None && !Main.wallHouse[wallType];
		}

		return false;
	}
}