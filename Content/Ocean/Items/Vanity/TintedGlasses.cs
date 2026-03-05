namespace SpiritReforged.Content.Ocean.Items.Vanity;

[AutoloadEquip(EquipType.Head)]
public class TintedGlasses : ModItem
{
	public override void SetStaticDefaults()
	{
		ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.Sunglasses;
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.buyPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}