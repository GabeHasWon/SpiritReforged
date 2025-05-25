namespace SpiritReforged.Content.Underground.WayfarerSet;

[AutoloadEquip(EquipType.Body, EquipType.Back)]
public class WayfarerBody : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Body.Sets.NeedsToDrawArm[Item.bodySlot] = true;
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.value = Item.sellPrice(0, 0, 40, 0);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 3;
	}

	public override void UpdateEquip(Player player)
	{
		player.moveSpeed += 0.05f;
		player.runAcceleration += 0.01f;
	}

	public override void EquipFrameEffects(Player player, EquipType type)
	{
		var bodyVanitySlot = player.armor[11];

		if (bodyVanitySlot.IsAir || bodyVanitySlot.type == Type)
			player.back = (sbyte)EquipLoader.GetEquipSlot(Mod, nameof(WayfarerBody), EquipType.Back);
	}
}