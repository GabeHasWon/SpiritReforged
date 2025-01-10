﻿using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Vanilla.Leather.LeatherCloak;

[AutoloadEquip(EquipType.Front)]
internal class LeatherCloakItem : AccessoryItem
{
	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 26;
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
		Item.value = Item.sellPrice(0, 0, 5, 0);
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (player.ZoneDesert)
			player.buffImmune[BuffID.WindPushed] = true;
	}

	public override void AddRecipes() => CreateRecipe()
			.AddIngredient(ItemID.Leather, 6)
			.AddIngredient(ItemID.Silk, 5)
			.AddTile(TileID.WorkBenches)
			.Register();
}