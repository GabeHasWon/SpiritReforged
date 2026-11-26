using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Underground.Items;

public class ChokeTube : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();

		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 50);
	}

	public override void UpdateEquip(Player player) => player.GetModPlayer<StatsPlayer>().Inaccuracy *= 0.5f;
}