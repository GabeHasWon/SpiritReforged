using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;

namespace SpiritReforged.Content.Ziggurat.Biome;

public class ZigguratBiome : ModBiome
{
	/// <summary> A collection of walls associated with the natural biome. </summary>
	public static readonly HashSet<int> WallTypes = [RedSandstoneBrickWall.UnsafeType, ModContent.WallType<RedSandstoneBrickForegroundWall>(), CarvedLapisWall.UnsafeType, PaleHiveWall.UnsafeType, ModContent.WallType<SandyZigguratWall>(), BronzePlatingWall.UnsafeType];
	/// <summary> A collection of tiles associated with the natural biome. Used only for <see cref="Common.WorldGeneration.QuickConversion."/> </summary>
	public static readonly HashSet<int> TileTypes = [ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>()];

	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Ziggurat");
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
	public override string MapBackground => "SpiritReforged/Assets/Textures/Backgrounds/ZigguratMapBG";

	public override bool IsBiomeActive(Player player)
	{
		int wallType = Framing.GetTileSafely(player.Center).WallType;
		return WallTypes.Contains(wallType);
	}
}