using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using System.Linq;

namespace SpiritReforged.Content.Ziggurat.Biome;

internal class ZigguratGlobalNPC : GlobalNPC
{
	public static bool InBiome(NPCSpawnInfo spawnInfo)
	{
		Point tileCoords = new(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1);
		int wallType = Framing.GetTileSafely(tileCoords).WallType;

		return !Main.wallHouse[wallType] && ZigguratMicrobiome.TotalBounds.Any(x => x.Contains(tileCoords));
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		if (InBiome(spawnInfo))
		{
			pool[0] = 0;
			pool[NPCID.SandSlime] = 0.05f;
		}
	}
}
