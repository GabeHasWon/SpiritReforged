using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Ocean.Items.Pearl;

[AutoloadEquip(EquipType.Neck)]
public class PearlString : EquippableItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.WhitePearl;
		DiscoveryHelper.RegisterPickup(Type, SoundID.CoinPickup with { Pitch = .25f });
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 20;
		Item.accessory = true;
		Item.value = Item.sellPrice(gold: 10);
		Item.rare = ItemRarityID.Blue;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		base.UpdateAccessory(player, hideVisual);

		player.luck += .15f;
		player.GetModPlayer<CoinLootPlayer>().AddMult(10);
	}
}