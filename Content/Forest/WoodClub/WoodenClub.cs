﻿using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Forest.WoodClub;

public class WoodenClub : ClubItem
{
	public override void SafeSetDefaults()
	{
		Item.damage = 18;
		Item.knockBack = 5;
		ChargeTime = 40;
		SwingTime = 24;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 1, 0);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<WoodenClubProj>();
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Wood, 30).AddTile(TileID.WorkBenches).Register();
}