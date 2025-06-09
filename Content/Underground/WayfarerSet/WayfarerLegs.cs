namespace SpiritReforged.Content.Underground.WayfarerSet;

[AutoloadEquip(EquipType.Legs)]
public class WayfarerLegs : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 18;
		Item.value = Item.sellPrice(0, 0, 40, 0);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 2;
	}

	public override void UpdateEquip(Player player)
	{
		player.moveSpeed += 0.07f;
		player.runAcceleration += .015f;
	}
}