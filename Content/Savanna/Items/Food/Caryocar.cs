using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Savanna.Items.Food;

public class Caryocar : FoodItem
{
	internal override Point Size => new(26, 26);
	public override void StaticDefaults() => FruitItemsSet.Add(Type);
}

