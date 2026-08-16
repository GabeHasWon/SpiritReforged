namespace SpiritReforged.Content.Forest.Mage;

[AutoloadEquip(EquipType.Shoes)]
public class ComfySlippers : ModItem
{
	public const int MANA_INCREASE = 40;
	public const float MANA_REDUCTION = 0.08f;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MANA_INCREASE, (int)(MANA_REDUCTION * 100f));

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override void UpdateEquip(Player player)
	{
		player.statManaMax2 += MANA_INCREASE;
		player.manaCost -= MANA_REDUCTION;
	}
}