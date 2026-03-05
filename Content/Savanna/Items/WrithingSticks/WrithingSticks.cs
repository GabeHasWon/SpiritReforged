using SpiritReforged.Common.BuffCommon;

namespace SpiritReforged.Content.Savanna.Items.WrithingSticks;

public class WrithingSticks : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<HuntingRifle.HuntingRifle>();
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<TumblerPet>();
		Item.buffType = AutoloadedPetBuff.Registered[Item.shoot];
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[0].IsAir;
}