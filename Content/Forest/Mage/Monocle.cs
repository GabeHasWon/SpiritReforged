namespace SpiritReforged.Content.Forest.Mage;

[AutoloadEquip(EquipType.Face)]
public class Monocle : ModItem
{
	public const float DAMAGE_INCREASE = 0.06f;
	public const float SPELL_BOOK_INCREASE = 0.2f;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(DAMAGE_INCREASE * 100f), (int)(SPELL_BOOK_INCREASE * 100f));

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override void UpdateEquip(Player player)
	{
		player.GetDamage(DamageClass.Magic) += DAMAGE_INCREASE;
	}
}