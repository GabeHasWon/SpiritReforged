using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet;

public class DriftwoodHammer : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.ShadewoodHammer);
		Item.hammer = 39;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 8).AddTile(TileID.WorkBenches).Register();
}
