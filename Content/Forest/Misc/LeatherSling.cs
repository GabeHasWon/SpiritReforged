using SpiritReforged.Common.ItemCommon.MagazineSystem;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadEquip(EquipType.Front)]
public class LeatherSling : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
	{
		if (equippedItem.ModItem is not null and Bandolier && incomingItem.ModItem is not null and LeatherSling || equippedItem.ModItem is not null and LeatherSling && incomingItem.ModItem is not null and Bandolier)
			return false;

		return true;
	}

	public override void UpdateEquip(Player player)
	{
		var modPlayer = player.GetModPlayer<MagazinePlayer>();

		modPlayer.inventoryReload = true;
		modPlayer.inventoryReloadTimerMax = 3;
	}
}

