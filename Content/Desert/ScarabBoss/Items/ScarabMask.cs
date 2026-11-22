namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

[AutoloadEquip(EquipType.Head)]
public class ScarabMask : ModItem
{
	public override void SetDefaults()
	{
		Item.width = Item.height = 22;
		Item.value = Item.sellPrice(silver: 75);
		Item.rare = ItemRarityID.Blue;
		Item.vanity = true;
	}
}