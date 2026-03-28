using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Content.Ziggurat;

namespace SpiritReforged.Content.Forest.Backpacks;

[AutoloadEquip(EquipType.Back, EquipType.Front)]
internal class FashionableBackpack : BackpackItem
{
	protected override int SlotCap => 4;
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<HikingBackpack>();
	public override void Defaults()
	{
		Item.Size = new Vector2(34, 28);
		Item.value = Item.buyPrice(0, 0, 5, 0);
		Item.rare = ItemRarityID.Blue;
	}
}
