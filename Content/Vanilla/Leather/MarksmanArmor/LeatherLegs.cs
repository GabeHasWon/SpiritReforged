namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

[AutoloadEquip(EquipType.Legs)]
public class LeatherLegs : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AncientMarksmanLegs>();

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 18;
		Item.value = Item.sellPrice(silver: 25);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}
}
