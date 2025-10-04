namespace SpiritReforged.Content.Underground.Items.EarthshakerVanity;

[AutoloadEquip(EquipType.Body)]
public class EarthshakerBreastplate : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<EarthshakerChestpiece>();

	public override void SetDefaults()
	{
		Item.width = 38;
		Item.height = 20;
		Item.value = Item.sellPrice(gold: 2);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
