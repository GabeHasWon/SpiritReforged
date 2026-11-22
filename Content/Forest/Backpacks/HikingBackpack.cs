using SpiritReforged.Common.ItemCommon.Backpacks;

namespace SpiritReforged.Content.Forest.Backpacks;

[AutoloadEquip(EquipType.Back, EquipType.Front)]
public class HikingBackpack : BackpackItem
{
	public override void SetDefaults()
	{
		Item.Size = new Vector2(34, 28);
		Item.value = Item.sellPrice(0, 0, 10, 0);
		Item.rare = ItemRarityID.Blue;

		slotCount = 4;
	}
}