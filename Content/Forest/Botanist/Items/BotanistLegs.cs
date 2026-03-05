namespace SpiritReforged.Content.Forest.Botanist.Items;

[AutoloadEquip(EquipType.Legs)]
public class BotanistLegs : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.value = Item.sellPrice(0, 0, 10, 0);
		Item.rare = ItemRarityID.White;
		Item.defense = 1;
	}

	public override void UpdateEquip(Player player) => player.moveSpeed += 0.1f;
}
