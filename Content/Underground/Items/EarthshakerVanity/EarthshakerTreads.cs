namespace SpiritReforged.Content.Underground.Items.EarthshakerVanity;

[AutoloadEquip(EquipType.Legs)]
public class EarthshakerTreads : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 18;
		Item.value = Item.sellPrice(silver: 80);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}
