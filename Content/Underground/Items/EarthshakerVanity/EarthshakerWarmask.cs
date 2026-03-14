namespace SpiritReforged.Content.Underground.Items.EarthshakerVanity;

[AutoloadEquip(EquipType.Head)]
public class EarthshakerWarmask : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<EarthshakerHeadgear>();

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
