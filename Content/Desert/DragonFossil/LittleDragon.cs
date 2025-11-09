using SpiritReforged.Common.BuffCommon;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class LittleDragon : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<LittleDragonPet>();
		Item.buffType = AutoloadedPetBuff.Registered[Item.shoot];
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[0].IsAir;
}