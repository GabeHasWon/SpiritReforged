using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Forest.Stargrass.Items;

public class CrescentMelon : FoodItem
{
	internal override Point Size => new(26, 28);
	public override void StaticDefaults() => SetFruitType();

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.ManaRegeneration, 3600);
		return true;
	}
}
