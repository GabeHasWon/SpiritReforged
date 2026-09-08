using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Underground.Items;

namespace SpiritReforged.Content.Forest.Misc;
public class GameHuntingKit : ModItem
{
	// TODO: Texture
	public override string Texture => AssetLoader.EmptyTexture;
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();

		Item.rare = ItemRarityID.Orange;
		Item.value = Item.sellPrice(gold: 2);
	}

	public override void UpdateEquip(Player player)
	{
		player.GetModPlayer<ShotgunPlayer>().shotgunStats._spreadMultiplier -= 0.5f;
		// If the player is holding a shotgun and it is also a magazine item, increase their magazine size by 50%
		// Works as expected with weapon swapping
		if (player.HeldItem.ModItem is not null and ShotgunItem shotgun && shotgun.Item.TryGetGlobalItem<MagazineGlobalItem>(out var result) && result.Active)
			player.GetModPlayer<MagazinePlayer>().magazineSizeMultiplier += 0.5f;
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
