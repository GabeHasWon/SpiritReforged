namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet.DriftwoodArmor;

[AutoloadEquip(EquipType.Body)]
public class DriftwoodChestplate : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 38;
		Item.height = 26;
		Item.value = Item.sellPrice(0, 0, 0, 0);
		Item.rare = ItemRarityID.White;
		Item.defense = 2;
	}
}
