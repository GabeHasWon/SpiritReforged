namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

[AutoloadEquip(EquipType.Legs, EquipType.Waist)]
public class BedouinLeggings : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 18;
		Item.value = 10000;
		Item.rare = ItemRarityID.Green;
		Item.defense = 3;
	}

	public override void UpdateEquip(Player player) => player.moveSpeed += 0.1f;

	public override void EquipFrameEffects(Player player, EquipType type)
	{
		if (player.waist == -1)
			player.waist = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Waist);
	}
}