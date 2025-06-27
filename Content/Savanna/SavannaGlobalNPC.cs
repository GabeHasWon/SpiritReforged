using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.NPCs.Sparrow;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Savanna.Tiles.Paintings;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna;

public class SavannaGlobalNPC : GlobalNPC
{
	public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
	{
		if (npc.type == NPCID.Dryad && (Main.LocalPlayer.ZoneDesert || Main.LocalPlayer.InModBiome<SavannaBiome>() || Main.LocalPlayer.ZoneJungle))
		{
			var grassSeeds = items.FirstOrDefault(x => x != null && x.type == ItemID.GrassSeeds);

			if (grassSeeds != default)
				grassSeeds.SetDefaults(ModContent.ItemType<Items.SavannaGrassSeeds>());
		}
	}

	public override void ModifyShop(NPCShop shop)
	{
		if (shop.NpcType == NPCID.Painter)
		{
			shop.Add(Mod.Find<ModItem>(nameof(DustyFields) + "Item").Type, Condition.MoonPhaseFull, SpiritConditions.InSavanna);
			shop.Add(Mod.Find<ModItem>(nameof(DustyFields) + "Item").Type, Condition.MoonPhaseWaningGibbous, SpiritConditions.InSavanna);

			shop.Add(Mod.Find<ModItem>(nameof(OrangeSkies) + "Item").Type, Condition.MoonPhaseThirdQuarter, SpiritConditions.InSavanna);
			shop.Add(Mod.Find<ModItem>(nameof(OrangeSkies) + "Item").Type, Condition.MoonPhaseWaningCrescent, SpiritConditions.InSavanna);

			shop.Add(Mod.Find<ModItem>(nameof(StrongWinds) + "Item").Type, Condition.MoonPhaseNew, SpiritConditions.InSavanna);
			shop.Add(Mod.Find<ModItem>(nameof(StrongWinds) + "Item").Type, Condition.MoonPhaseWaxingCrescent, SpiritConditions.InSavanna);

			shop.Add(Mod.Find<ModItem>(nameof(WaningSun) + "Item").Type, Condition.MoonPhaseFirstQuarter, SpiritConditions.InSavanna);
			shop.Add(Mod.Find<ModItem>(nameof(WaningSun) + "Item").Type, Condition.MoonPhaseWaxingGibbous, SpiritConditions.InSavanna);
		}
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Invasion)
			return;

		if (spawnInfo.SpawnTileType == ModContent.TileType<SavannaGrass>())
		{
			if (spawnInfo.PlayerInTown)
			{
				pool[NPCID.Vulture] = .009f; //Vultures can sometimes spawn, as a treat

				if (Main.dayTime)
					pool[NPCID.Bird] = .07f;

				return;
			}

			if (Main.dayTime)
				pool.Remove(0); //Remove all vanilla spawns

			if (Main.raining)
			{
				pool[NPCID.FlyingFish] = Main.dayTime ? .25f : .18f;
				pool[NPCID.UmbrellaSlime] = Main.dayTime ? .17f : .08f;
			}

			if (!Main.dayTime)
			{
				pool[NPCID.DoctorBones] = .007f;
			}
			else if (!spawnInfo.Player.GetModPlayer<DustStorm.DustStormPlayer>().ZoneDustStorm)
			{
				pool[NPCID.Pinky] = .007f;
				pool[NPCID.Bird] = .05f;
			}

			float odds = spawnInfo.Player.GetModPlayer<DustStorm.DustStormPlayer>().ZoneDustStorm ? .22f : .12f;
			pool[NPCID.Vulture] = odds;
		}
		else if (spawnInfo.SpawnTileType == ModContent.TileType<SavannaGrassCrimson>())
		{
			pool.Remove(0); //Remove all vanilla spawns

			if (Main.hardMode)
			{
				pool[NPCID.Crimslime] = .1f;
				pool[NPCID.Herpling] = .125f;
			}

			pool[NPCID.BloodCrawler] = .1f;
			pool[NPCID.FaceMonster] = .34f;
			pool[NPCID.Crimera] = .3f;
		}
		else if (spawnInfo.SpawnTileType == ModContent.TileType<SavannaGrassCorrupt>() || InCorruption())
		{
			pool.Remove(0); //Remove all vanilla spawns

			if (InCorruption() && spawnInfo.SpawnTileY > spawnInfo.PlayerFloorY)
			{
				if (Main.hardMode)
					pool[NPCID.SeekerHead] = .03f;
				else
					pool[NPCID.DevourerHead] = .05f;
			}

			if (Main.hardMode)
			{
				pool[NPCID.Slimer] = .1f;
				pool[NPCID.Corruptor] = .4f;
			}

			pool[NPCID.EaterofSouls] = .4f;
		}
		else if (spawnInfo.SpawnTileType == ModContent.TileType<SavannaGrassHallow>())
		{
			pool.Remove(0); //Remove all vanilla spawns

			pool[NPCID.Pixie] = .5f;
			pool[NPCID.Unicorn] = .28f;

			if (!Main.dayTime)
				pool[NPCID.Gastropod] = .4f;

			if (Main.raining)
				pool[NPCID.RainbowSlime] = .055f;

			if (NPC.downedPlantBoss)
				pool[NPCID.EmpressButterfly] = .005f;
		}

		bool InCorruption() => spawnInfo.Player.InModBiome<SavannaBiome>() && spawnInfo.Player.ZoneCorrupt;
	}

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if ((npc.type == NPCID.Vulture || npc.type == NPCID.Bird || npc.type == ModContent.NPCType<Sparrow>()) && source is EntitySource_SpawnNPC)
		{
			//Move to an acacia treetop within 40 tiles when naturally spawned
			var nearby = Tiles.AcaciaTree.AcaciaTree.Platforms.Where(x => x.Distance(npc.Center) < 16 * 40).OrderBy(x => x.Distance(npc.Center)).FirstOrDefault();
			if (nearby != default)
			{
				var toPos = nearby.Hitbox.ClosestPointInRect(npc.Center);
				if (!WorldGen.PlayerLOS((int)(toPos.X / 16), (int)(toPos.Y / 16)))
				{
					npc.Center = toPos;
					npc.netUpdate = true;
				}
			}
		}
	}
}
