using SpiritReforged.Common.ItemCommon.Backpacks;

namespace SpiritReforged.Content.Forest.Backpacks;

[AutoloadEquip(EquipType.Back, EquipType.Front)]
internal class PouchPack : BackpackItem
{
	protected override int SlotCap => 4;

	public override void Defaults()
	{
		Item.Size = new Vector2(34, 28);
		Item.value = Item.buyPrice(0, 0, 5, 0);
		Item.rare = ItemRarityID.Blue;
	}
}
