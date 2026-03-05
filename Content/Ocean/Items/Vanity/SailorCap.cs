namespace SpiritReforged.Content.Ocean.Items.Vanity;

[AutoloadEquip(EquipType.Head)]
public class SailorCap : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 22;
		Item.value = Item.sellPrice(0, 0, 6, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
