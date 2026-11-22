using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.Misc;

[FromClassic("SwiftRune")]
public class CraneFeather : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemLootDatabase.AddItemRule(ItemID.WoodenCrate, ItemDropRule.Common(Type, 20));
		ItemLootDatabase.AddItemRule(ItemID.WoodenCrateHard, ItemDropRule.Common(Type, 20));
	}

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
