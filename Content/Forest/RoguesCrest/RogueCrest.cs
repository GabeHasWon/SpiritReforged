using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.RoguesCrest;

[AutoloadEquip(EquipType.Neck)]
public class RogueCrest : MinionAccessory
{
	public override MinionAccessoryData Data => new(ModContent.ProjectileType<RogueKnifeMinion>(), 5);

	public override void StaticDefaults()
	{
		ItemLootDatabase.AddItemRule(ItemID.WoodenCrate, ItemDropRule.Common(Type, 8));
		ItemLootDatabase.AddItemRule(ItemID.WoodenCrateHard, ItemDropRule.Common(Type, 8));
	}

	public override void Defaults()
	{
		Item.width = 38;
		Item.height = 36;
		Item.value = Item.buyPrice(0, 3, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;

		MoRHelper.SetSlashBonus(Item);
	}
}