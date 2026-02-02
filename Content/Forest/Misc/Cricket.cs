using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadCritter]
public class Cricket : ModNPC
{
	public sealed class CricketSpawnNPC : GlobalNPC
	{
		public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
		{
			if (pool.TryGetValue(ModContent.NPCType<Cricket>(), out float chance) && chance != 0)
				pool[NPCID.Grasshopper] = 0; //Completely replace normal Grasshopper spawns
		}
	}

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 2;

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Grasshopper);
		AnimationType = NPCID.Grasshopper;
	}

	public virtual void CreateItemDefaults() => 
		ItemEvents.CreateItemDefaults(
		this.AutoItemType(), 
		static item =>
		{
			item.value = Item.sellPrice(0, 0, 0, 45);
			item.bait = 10;
		}
	);

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (!Main.dayTime && spawnInfo.Common() && !spawnInfo.Water 
		&& (spawnInfo.SpawnTileType == TileID.Grass || spawnInfo.SpawnTileType == TileID.HallowedGrass) 
		&& Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) < Main.maxTilesX / 3) 
		? 0.1f : 0;
}