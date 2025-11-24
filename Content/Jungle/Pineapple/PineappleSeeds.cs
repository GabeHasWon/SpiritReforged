using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Jungle.Pineapple;

public class PineappleSeeds : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.WitchDoctor, new NPCShop.Entry(Item.type)));
		ItemLootDatabase.ModifyItemRule(ItemID.HerbBag, AddTypesToList);
		Item.ResearchUnlockCount = 25;
	}

	/// <summary> Adds Pineapple Seeds to the Herb Bag drop pool. </summary>
	private static void AddTypesToList(ref ItemLoot loot)
	{
		foreach (var rule in loot.Get())
		{
			if (rule is HerbBagDropsItemDropRule herbRule)
			{
				var drops = herbRule.dropIds.ToList();
				drops.AddRange([ModContent.ItemType<PineappleSeeds>()]);

				herbRule.dropIds = [.. drops];
				return;
			}
		}
	}

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<PineapplePlant>());
		Item.width = 20;
		Item.height = 20;
		Item.value = Item.sellPrice(silver: 1);
	}
}