namespace SpiritReforged.Content.Forest.Botanist.Items;

[AutoloadEquip(EquipType.Body, EquipType.Waist)]
public class BotanistBody : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.value = Item.sellPrice(0, 0, 10, 0);
		Item.rare = ItemRarityID.White;
		Item.defense = 1;
	}

	public override void EquipFrameEffects(Player player, EquipType type)
	{
		var bodyVanitySlot = player.armor[11];

		if (bodyVanitySlot.IsAir || bodyVanitySlot.type == Type)
			player.waist = EquipLoader.GetEquipSlot(Mod, nameof(BotanistBody), EquipType.Waist);
	}
}