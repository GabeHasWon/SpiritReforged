using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Common.ModCompat.Classic;

/// <summary> Modifies Classic NPC shops, spawn rates, and drop tables. </summary>
internal class ModifyNPCData : GlobalNPC
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Classic.Enabled;

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		foreach (var rule in npcLoot.Get())
		{
			if (rule is CommonDrop drop && SpiritClassic.ClassicItemToReforged.TryGetValue(drop.itemId, out int reforged))
				drop.itemId = reforged;
		}
	}

	public override void ModifyShop(NPCShop shop)
	{
		foreach (var entry in shop.ActiveEntries)
		{
			if (SpiritClassic.ClassicItemToReforged.TryGetValue(entry.Item.type, out int reforgedType))
				entry.Item.ChangeItemType(reforgedType);
		}
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		foreach (var entry in pool)
		{
			if (SpiritClassic.ClassicNPCToReforged.ContainsKey(entry.Key))
				pool[entry.Key] = 0f; //Disable spawn
		}
	}
}