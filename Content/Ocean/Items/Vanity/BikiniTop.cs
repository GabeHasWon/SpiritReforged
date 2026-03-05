namespace SpiritReforged.Content.Ocean.Items.Vanity;

[AutoloadEquip(EquipType.Body)]
public class BikiniTop : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<BikiniBottom>();
	
	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 20;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}