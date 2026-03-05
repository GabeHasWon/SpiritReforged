namespace SpiritReforged.Content.Ocean;

internal class OceanGlobalItem : GlobalItem
{
	public override void SetDefaults(Item item)
	{
		if (item.type == Mod.Find<ModItem>("TubewormItem").Type)
		{
			item.value = Item.sellPrice(copper: 10);
			item.rare = ItemRarityID.Blue;
			item.bait = 8;
		}
		else if (item.type == Mod.Find<ModItem>("CrinoidItem").Type)
		{
			item.value = Item.sellPrice(copper: 30);
			item.rare = ItemRarityID.Blue;
			item.bait = 25;
		}
		else if (item.type == Mod.Find<ModItem>("TinyCrabItem").Type)
		{
			item.value = Item.sellPrice(copper: 20);
			item.rare = ItemRarityID.Blue;
			item.bait = 15;
		}
	}

	public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
	{
		if (item.type is ItemID.OceanCrate or ItemID.OceanCrateHard)
			itemLoot.AddCommon(ModContent.ItemType<Items.Lifesaver.Lifesaver>(), 12);
	}
}
