using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Savanna.Items.Food;

public class OstrichEgg : FoodItem
{
	internal override Point Size => new(24, 26);

	public override void Defaults()
	{
		Item.buffType = BuffID.WellFed;
		Item.buffTime = 2 * 60 * 60;
	}
}
