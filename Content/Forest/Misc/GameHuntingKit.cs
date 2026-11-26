using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Underground.Items;

namespace SpiritReforged.Content.Forest.Misc;

public class GameHuntingKit : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.sellPrice(gold: 2);
	}

	public override void UpdateEquip(Player player)
	{
		player.GetModPlayer<StatsPlayer>().Inaccuracy *= 0.5f;

		if (player.HeldItem.CountsAsClass<ShotgunClass>()) //The item is a shotgun
			player.GetModPlayer<MagazinePlayer>().magazineScale += 0.4f;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient<ChokeTube>()
		.AddIngredient<SuperExtendoMags>()
		.AddTile(TileID.TinkerersWorkbench)
		.Register();
}