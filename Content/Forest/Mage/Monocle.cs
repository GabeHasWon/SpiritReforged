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

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(DAMAGE_INCREASE * 100f), (int)(SPELL_BOOK_INCREASE * 100f));

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override void UpdateEquippable(Player player) => player.GetDamage(DamageClass.Magic) += DAMAGE_INCREASE;
}