namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

[AutoloadEquip(EquipType.Body)]
public class LeatherPlate : ModItem
{
	public override void SetStaticDefaults()
	{
		ArmorIDs.Body.Sets.NeedsToDrawArm[Item.bodySlot] = true;
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AncientMarksmanPlate>();
	}

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 18;
		Item.value = Item.sellPrice(silver: 50);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 2;
	}
}
