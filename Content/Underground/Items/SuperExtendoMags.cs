using SpiritReforged.Common.ItemCommon.MagazineSystem;

namespace SpiritReforged.Content.Underground.Items;

public class SuperExtendoMags : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.sellPrice(gold: 2);
	}

	public override void UpdateEquip(Player player)
	{
		if (player.TryGetModPlayer(out MagazineInterfacePlayer mPlayer))
		{
			mPlayer.magazineScale += 1;
			mPlayer.reloadSpeed += 0.3f;
		}
	}
}