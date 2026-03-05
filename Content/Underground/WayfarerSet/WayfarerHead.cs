using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Underground.WayfarerSet;

[AutoloadEquip(EquipType.Head)]
public class WayfarerHead : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 24;
		Item.value = Item.sellPrice(0, 0, 60, 0);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs) => head.type == Type && body.type == ModContent.ItemType<WayfarerBody>() && legs.type == ModContent.ItemType<WayfarerLegs>();
	public override void UpdateEquip(Player player) => player.buffImmune[BuffID.Darkness] = true;

	public override void UpdateArmorSet(Player player)
	{
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Wayfarer");
		player.GetModPlayer<CoinLootPlayer>().AddMult(10);
	}
}