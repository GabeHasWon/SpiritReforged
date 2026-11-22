using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Common.ItemCommon;

internal class ItemLootDatabase : GlobalItem
{
	public delegate void ModifyLoot(ref ItemLoot itemLoot);

	public readonly record struct ItemLootDrop(int ItemType, IItemDropRule Rule);

	internal static readonly Dictionary<int, ModifyLoot> LootDelegates = [];
	internal static List<ItemLootDrop> ItemRules = [];

	public static void AddItemRule(int itemType, IItemDropRule rule) => ItemRules.Add(new ItemLootDrop(itemType, rule));
	public static void ModifyItemRule(int itemType, ModifyLoot rule)
	{
		if (!LootDelegates.TryAdd(itemType, rule))
			LootDelegates[itemType] += rule;
	}

	public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
	{
		foreach (var rule in ItemRules)
			if (item.type == rule.ItemType)
				itemLoot.Add(rule.Rule);

		if (LootDelegates.TryGetValue(item.type, out var del))
			del.Invoke(ref itemLoot);
	}
}
