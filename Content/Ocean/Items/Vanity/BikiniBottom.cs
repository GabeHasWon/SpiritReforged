namespace SpiritReforged.Content.Ocean.Items.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class BikiniBottom : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<SwimmingTrunks>();

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 12;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}