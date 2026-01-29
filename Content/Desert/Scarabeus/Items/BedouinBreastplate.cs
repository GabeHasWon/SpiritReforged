namespace SpiritReforged.Content.Desert.Scarabeus.Items;

[AutoloadEquip(EquipType.Body, EquipType.Back)]
public class BedouinBreastplate : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 30;
		Item.value = 12500;
		Item.rare = ItemRarityID.Green;
		Item.defense = 4;
	}

	public override void UpdateEquip(Player player) => player.GetDamage(DamageClass.Generic) += 0.05f;

	public override void EquipFrameEffects(Player player, EquipType type)
	{
		if (player.back == -1)
			player.back = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);
	}
}