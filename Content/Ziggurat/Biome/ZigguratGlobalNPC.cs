namespace SpiritReforged.Content.Ziggurat.Biome;

internal class ZigguratGlobalNPC : GlobalNPC
{
	public static bool InBiome(NPCSpawnInfo spawnInfo) => ZigguratBiome.WallTypes.Contains(Framing.GetTileSafely(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1).WallType);

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		if (InBiome(spawnInfo))
		{
			pool[0] = 0;
			pool[NPCID.SandSlime] = 0.05f;
		}
	}
}
