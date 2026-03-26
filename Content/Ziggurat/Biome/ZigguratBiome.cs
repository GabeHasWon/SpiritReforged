using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;
using System.Runtime.CompilerServices;
using System.Linq;

namespace SpiritReforged.Content.Ziggurat.Biome;

public class ZigguratBiome : ModBiome
{
	public class ZigguratCounts : ILoadable
	{
		public void Load(Mod mod) => MonoModHooks.Add(typeof(TileLoader).GetMethod(nameof(TileLoader.RecountTiles)), RecountTiles);

		public static void RecountTiles(Action<SceneMetrics> orig, SceneMetrics metrics)
		{
			orig(metrics);

			int[] tileCounts = GetTileCounts(metrics);
			metrics.SandTileCount += tileCounts[ModContent.TileType<RedSandstoneBrick>()] * 2;
			metrics.SandTileCount += tileCounts[ModContent.TileType<RedSandstoneBrickCracked>()] * 2;
			metrics.SandTileCount += tileCounts[ModContent.TileType<RedSandstoneSlab>()] * 2;
		}

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_tileCounts")]
		public static extern ref int[] GetTileCounts(SceneMetrics metrics);

		public void Unload() { }
	}

	/// <summary> A collection of walls associated with the natural biome. </summary>
	public static readonly HashSet<int> WallTypes = [RedSandstoneBrickWall.UnsafeType, ModContent.WallType<RedSandstoneBrickForegroundWall>(), CarvedLapisWall.UnsafeType, PaleHiveWall.UnsafeType, ModContent.WallType<SandyZigguratWall>(), BronzePlatingWall.UnsafeType];
	/// <summary> A collection of tiles associated with the natural biome. Used only for <see cref="Common.WorldGeneration.QuickConversion."/> </summary>
	public static readonly HashSet<int> TileTypes = [ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>()];

	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Ziggurat");
	public override SceneEffectPriority Priority => SceneEffectPriority.Event;
	public override string MapBackground => "SpiritReforged/Assets/Textures/Backgrounds/ZigguratMapBG";
	public override string BackgroundPath => MapBackground;

	public override bool IsBiomeActive(Player player)
	{
		Point tileCoords = player.Center.ToTileCoordinates();
		int wallType = Framing.GetTileSafely(tileCoords).WallType;

		return !Main.wallHouse[wallType] && ZigguratMicrobiome.TotalBounds.Any(x => x.Contains(tileCoords));
	}

	public override void OnInBiome(Player player) => player.ZoneDesert = true;
}