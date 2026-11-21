using SpiritReforged.Common.ItemCommon.MagazineSystem;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadEquip(EquipType.Body)]
public class LeatherSling : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
	{
		if (equippedItem.ModItem is Bandolier && incomingItem.ModItem is LeatherSling || equippedItem.ModItem is LeatherSling && incomingItem.ModItem is not null and Bandolier)
			return false;

		return true;
	}

	public override void UpdateEquip(Player player)
	{
		var modPlayer = player.GetModPlayer<MagazinePlayer>();

		//modPlayer.inventoryReload = true;
		//modPlayer.inventoryReloadTimerMax = 3;
	}
}