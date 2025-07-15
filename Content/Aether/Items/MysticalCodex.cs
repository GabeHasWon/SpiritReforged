using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Aether.Items;

public class MysticalCodex : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Radar);
		Item.Size = new(30, 32);
	}

	public override void UpdateInfoAccessory(Player player)
	{
		var info = player.GetModPlayer<InfoPlayer>().info;

		info[ModContent.GetInstance<ScryingLens>().Name] = true;
		info[ModContent.GetInstance<Ledger>().Name] = true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient<Ledger>().AddIngredient<ScryingLens>().AddTile(TileID.TinkerersWorkbench).Register();
}