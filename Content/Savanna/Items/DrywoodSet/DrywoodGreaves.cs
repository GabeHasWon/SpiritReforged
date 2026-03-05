namespace SpiritReforged.Content.Savanna.Items.DrywoodSet;

[AutoloadEquip(EquipType.Legs)]
public class DrywoodGreaves : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 18;
		Item.value = Item.sellPrice(copper: 9);
		Item.defense = 1;
	}
}
