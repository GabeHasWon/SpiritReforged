using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats;

public class SaltFlatsGlobalNPC : GlobalNPC
{
	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Invasion)
			return;

		if (spawnInfo.Player.InModBiome<SaltBiome>())
			pool.Remove(0);

		if (spawnInfo.SpawnTileType == ModContent.TileType<SaltBlock>() || spawnInfo.SpawnTileType == ModContent.TileType<SaltBlockDull>())
		{
			if (spawnInfo.PlayerInTown)
			{
				if (Main.dayTime)
				{
					pool[NPCID.RedDragonfly] = .08f;
					pool[NPCID.BirdRed] = .03f;
				}

				return;
			}

			if (Main.raining)
			{
				pool[NPCID.FlyingFish] = Main.dayTime ? .25f : .18f;
			}

			if (Main.dayTime)
			{
				pool[NPCID.RedDragonfly] = .05f;
				pool[NPCID.BirdRed] = .02f;
			}
			else
			{
				pool[NPCID.DemonEye] = .18f;
				pool[NPCID.SleepyEye2] = .07f;
				pool[NPCID.DialatedEye2] = .07f;
				pool[NPCID.CataractEye2] = .07f;
			}

			if (Main.hardMode && !Main.dayTime)
			{
				pool[NPCID.Wraith] = .25f;
				pool[NPCID.PossessedArmor] = .15f;
			}
		}
	}
}