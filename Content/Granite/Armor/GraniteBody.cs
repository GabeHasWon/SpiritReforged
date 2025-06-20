using SpiritReforged.Common.ModCompat.Classic;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Body)]
[FromClassic("GraniteChest")]
public class GraniteBody : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = 1100;
		Item.rare = ItemRarityID.Green;
		Item.defense = 11;
	}

	public override void UpdateEquip(Player player) => Player.jumpSpeed += 1;
}