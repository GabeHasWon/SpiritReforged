using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ModCompat.Classic;

namespace SpiritReforged.Content.Vanilla.Food;

[FromClassic("Cake")]
public class CarrotCake : FoodItem
{
	public override void Defaults()
	{
		Item.buffType = BuffID.WellFed3;
		Item.buffTime = 10 * 60 * 60;
		Item.value = Item.sellPrice(0, 0, 60, 0);
	}

	internal override Point Size => new(30, 38);
}
