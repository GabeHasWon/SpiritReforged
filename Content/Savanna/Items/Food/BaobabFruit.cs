using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Savanna.Items.Food;

public class BaobabFruit : FoodItem
{
	internal override Point Size => new(24, 24);
	public override void StaticDefaults() => SetFruitType();
}
