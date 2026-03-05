using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Forest.Stargrass.Items;

public class Pearlberry : FoodItem
{
	internal override Point Size => new(22, 26);
	public override void StaticDefaults() => SetFruitType();

	public override bool CanUseItem(Player player)
	{
		player.AddBuff(BuffID.ManaRegeneration, 3600);
		return true;
	}
}
