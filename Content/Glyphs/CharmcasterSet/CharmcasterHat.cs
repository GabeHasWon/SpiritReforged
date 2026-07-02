namespace SpiritReforged.Content.Glyphs.CharmcasterSet;

[AutoloadEquip(EquipType.Head)]
public class CharmcasterHat : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 30;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.White;

		Item.vanity = true;
	}
}
