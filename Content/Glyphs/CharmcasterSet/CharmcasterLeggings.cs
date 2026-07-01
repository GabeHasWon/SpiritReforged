namespace SpiritReforged.Content.Forest.Glyphs.CharmcasterSet;

[AutoloadEquip(EquipType.Legs)]
public class CharmcasterLeggings : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 30;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.White;

		Item.vanity = true;
	}
}
