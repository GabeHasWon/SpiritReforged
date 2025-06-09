using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.ArcaneNecklace;

[AutoloadEquip(EquipType.Neck)]
[FromClassic("ArcaneNecklace")]
public class ArcaneNecklaceGold : EquippableItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ArcaneNecklacePlatinum>();

		ItemLootDatabase.AddItemRule(ItemID.WoodenCrate, ItemDropRule.OneFromOptions(8, Type, ModContent.ItemType<ArcaneNecklacePlatinum>()));
		ItemLootDatabase.AddItemRule(ItemID.WoodenCrateHard, ItemDropRule.OneFromOptions(8, Type, ModContent.ItemType<ArcaneNecklacePlatinum>()));
	}

	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 34;
		Item.value = Item.sellPrice(0, 0, 25, 0);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual) => player.statManaMax2 += 20;
}
