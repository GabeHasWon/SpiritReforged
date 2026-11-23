using SpiritReforged.Common.ItemCommon.MagazineSystem;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadEquip(EquipType.Body)]
public class Bandolier : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Orange;
	}

	public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
	{
		if (equippedItem.ModItem is Bandolier && incomingItem.ModItem is LeatherSling || equippedItem.ModItem is  LeatherSling && incomingItem.ModItem is Bandolier)
			return false;

		return true;
	}

	public override void UpdateEquip(Player player)
	{
		MagazinePlayer modPlayer = player.GetModPlayer<MagazinePlayer>();
		modPlayer.inventoryReloadMultiplier = 2f;

		modPlayer.magazineScale += 0.5f;
		modPlayer.reloadLength -= 0.2f;
	}
}