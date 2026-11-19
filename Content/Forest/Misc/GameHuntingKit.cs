using SpiritReforged.Common.ItemCommon.MagazineSystem;
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
		player.GetModPlayer<ShotgunPlayer>().shotgunStats.spreadMultiplier -= 0.5f;
		Item item = player.HeldItem;

		if (ShotgunGlobalItem.Shotgun[item.type] != null) //The item is a shotgun
			player.GetModPlayer<MagazineInterfacePlayer>().magazineScale += 0.4f;
	}

	public override void AddRecipes()
	{
		CreateRecipe().
			AddIngredient<ChokeTube>().
			AddIngredient<SuperExtendoMags>().
			AddTile(TileID.TinkerersWorkbench).
			Register();
	}
}