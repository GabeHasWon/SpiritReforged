namespace SpiritReforged.Content.Ziggurat.Vanity;

[AutoloadEquip(EquipType.Head)]
public class AvianRitualMask : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<BullRitualMask>();
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}

[AutoloadEquip(EquipType.Head)]
public class BullRitualMask : AvianRitualMask
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AvianRitualMask>();
	public override void SetDefaults()
	{
		Item.Size = new(24);
		base.SetDefaults();
	}
}