using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Legs)]
[AutoloadGlowmask("255,255,255")]
public class GraniteLegs : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = 1100;
		Item.rare = ItemRarityID.Green;
		Item.defense = 10;
	}

	public override void UpdateEquip(Player player) => Player.jumpSpeed += 1;
}