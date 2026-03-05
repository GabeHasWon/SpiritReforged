using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SpiritReforged.Content.Ocean.Items.Vanity.DiverSet;

[AutoloadEquip(EquipType.Legs)]
public class DiverLegs : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 30;
		Item.value = Item.sellPrice(0, 0, 10, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
