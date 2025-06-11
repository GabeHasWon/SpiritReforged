using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Vanilla.Food;

public class Hummus : FoodItem
{
	internal override Point Size => new(38, 28);

	public override void Defaults()
	{
		Item.rare = ItemRarityID.Green;
		Item.buffTime = 30 * 60 * 60;
		Item.value = Item.sellPrice(0, 0, 45, 0);
	}
}
