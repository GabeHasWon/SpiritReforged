using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Vanilla.Food;

public class RawMeat : FoodItem
{
	internal override Point Size => new(30, 26);

	public override void StaticDefaults() => VariantItemRenderer.VariantCounts[Type] = 3;

	public override void Defaults() => Item.buffTime = 45 * 60;

	public override bool CanUseItem(Player player)
	{
		if (player.UsedQuickBuff())
			return false;

		player.AddBuff(BuffID.Poisoned, 45 * 60);
		return true;
	}
}