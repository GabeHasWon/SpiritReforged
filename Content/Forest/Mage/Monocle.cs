using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Forest.Mage;

[AutoloadEquip(EquipType.Head)]
public class Monocle : EquippableItem
{
	private sealed class MonocleBoostItem : GlobalItem
	{
		public override float UseSpeedMultiplier(Item item, Player player)
		{
			float value = 1f;

			if (player.HasEquip<Monocle>() && SpiritSets.MagicBook[item.type])
				value += SPELL_BOOK_INCREASE;

			return value;
		}
	}

	public const float DAMAGE_INCREASE = 0.06f;
	public const float SPELL_BOOK_INCREASE = 0.2f;
	public const float KNOCKBACK_BONUS = 0.2f;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(DAMAGE_INCREASE * 100f), (int)(SPELL_BOOK_INCREASE * 100f));

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs) => body.type is ItemID.AmberRobe or ItemID.AmethystRobe or ItemID.DiamondRobe or ItemID.EmeraldRobe or ItemID.RubyRobe or ItemID.SapphireRobe or ItemID.TopazRobe or ItemID.GypsyRobe;

	public override void UpdateArmorSet(Player player)
	{
		player.GetKnockback(DamageClass.Magic) += KNOCKBACK_BONUS;
		player.setBonus = this.GetLocalization("SetBonus").WithFormatArgs((int)(KNOCKBACK_BONUS * 100f)).Value;
	}

	public override void UpdateEquippable(Player player) => player.GetDamage(DamageClass.Magic) += DAMAGE_INCREASE;
}