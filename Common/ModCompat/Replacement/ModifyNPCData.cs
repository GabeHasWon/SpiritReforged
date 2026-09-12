using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Common.ModCompat.Replacement;

/// <summary> Modifies NPC shops, spawn rates, and drop tables with content replacements. </summary>
internal class ModifyNPCData : GlobalNPC
{
	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		foreach (IItemDropRule rule in npcLoot.Get())
		{
			if (rule is CommonDrop drop && ReplacementSystem.ItemToReplacement.TryGetValue(drop.itemId, out int reforged))
				drop.itemId = reforged;
		}
	}

	public override void ModifyShop(NPCShop shop)
	{
		foreach (NPCShop.Entry entry in shop.ActiveEntries)
		{
			if (ReplacementSystem.ItemToReplacement.TryGetValue(entry.Item.type, out int reforgedType))
				entry.Item.ChangeItemType(reforgedType);
		}
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		foreach (var entry in pool)
		{
			if (ReplacementSystem.ItemToReplacement.ContainsKey(entry.Key))
				pool[entry.Key] = 0f; //Disable spawn
		}
	}
}