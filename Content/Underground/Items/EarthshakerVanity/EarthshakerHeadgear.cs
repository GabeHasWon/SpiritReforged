namespace SpiritReforged.Content.Underground.Items.EarthshakerVanity;

[AutoloadEquip(EquipType.Head)]
public class EarthshakerHeadgear : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<EarthshakerWarmask>();

	public override void SetDefaults()
	{
		Item.width = 34;
		Item.height = 26;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
