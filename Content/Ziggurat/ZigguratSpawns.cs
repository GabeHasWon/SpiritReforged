using SpiritReforged.Content.Ziggurat.Biome;

namespace SpiritReforged.Content.Ziggurat;

internal class ZigguratSpawns : GlobalNPC
{
	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
	{
		if (!player.InModBiome<ZigguratBiome>() || spawnRate == int.MinValue) // minValue check is to avoid a DragonLens "no spawn rate" crash
			return;

		spawnRate = (int)(spawnRate * 0.7f);
		maxSpawns += 2;
	}
}
