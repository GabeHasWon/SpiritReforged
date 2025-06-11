using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Vanilla.Food;

public class RawMeat : FoodItem
{
	internal override Point Size => new(30, 26);

	public override void StaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<RawFish>();
		VariantGlobalItem.AddVariants(Type, [new Point(30, 26), new Point(28, 22), new Point(26, 22)]);
	}

	public override void Defaults() => Item.buffTime = 45 * 60;

	public override bool CanUseItem(Player player)
	{
		if (player.UsedQuickBuff())
			return false;

		player.AddBuff(BuffID.Poisoned, 45 * 60);
		return true;
	}

	public override bool PreDrawInWorld(SpriteBatch sb, Color light, Color a, ref float rotation, ref float scale, int whoAmI) => false;
}

