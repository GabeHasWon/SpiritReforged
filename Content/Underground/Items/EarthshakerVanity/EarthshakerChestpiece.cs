namespace SpiritReforged.Content.Underground.Items.EarthshakerVanity;

[AutoloadEquip(EquipType.Body)]
public class EarthshakerChestpiece : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 38;
		Item.height = 26;
		Item.value = Item.sellPrice(gold: 2);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
