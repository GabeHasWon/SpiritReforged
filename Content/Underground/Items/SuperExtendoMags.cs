using SpiritReforged.Common.ItemCommon.MagazineSystem;

namespace SpiritReforged.Content.Underground.Items;
public class SuperExtendoMags : ModItem
{
	// TODO: Obtainment
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();

		// Placeholders
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.sellPrice(gold: 2);
	}

	public override void UpdateEquip(Player player)
	{
		var mp = player.GetModPlayer<MagazinePlayer>();

		mp.magazineSizeMultiplier += 1f;
		mp.reloadTimeMultiplier += 0.3f;
	}
}
