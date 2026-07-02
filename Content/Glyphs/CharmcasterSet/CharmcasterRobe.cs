namespace SpiritReforged.Content.Glyphs.CharmcasterSet;

[AutoloadEquip(EquipType.Body)]
public class CharmcasterRobe : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 30;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.White;

		Item.vanity = true;
	}
}
