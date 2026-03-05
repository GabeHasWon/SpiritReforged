using SpiritReforged.Common.ModCompat.Classic;

namespace SpiritReforged.Content.Forest.Misc;

[FromClassic("SwiftRune")]
public class CraneFeather : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 34;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (player.velocity.Y != 0 && player.wings <= 0 && !player.mount.Active)
		{
			player.runAcceleration *= 2f;
			player.maxRunSpeed *= 1.5f;
		}
	}
}
