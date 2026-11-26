using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Forest.Misc;

public class HoloSight : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Blue;
	}

	public override void UpdateEquip(Player player)
	{
		player.GetModPlayer<StatsPlayer>().Inaccuracy -= 0.2f;
		player.GetModPlayer<StatsPlayer>().ShotSpeed += 0.2f;
		player.GetCritChance(DamageClass.Ranged) += 4;
	}
}