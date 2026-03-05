namespace SpiritReforged.Content.Forest.ArcaneNecklace;

[AutoloadEquip(EquipType.Neck)]
public class ArcaneNecklacePlatinum : ArcaneNecklaceGold
{
	// Gold variant handles crate entries
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ArcaneNecklaceGold>();

	public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => incomingItem.type != ModContent.ItemType<ArcaneNecklaceGold>();
}