namespace SpiritReforged.Content.Savanna.Items.DrywoodSet;

[AutoloadEquip(EquipType.Body)]
public class DrywoodBreastplate : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.value = Item.sellPrice(copper: 12);
		Item.defense = 1;
	}
}
