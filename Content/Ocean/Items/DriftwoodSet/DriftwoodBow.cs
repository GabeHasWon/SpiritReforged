using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet;

public class DriftwoodBow : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.ShadewoodBow);
		Item.damage = 8;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 10).AddTile(TileID.WorkBenches).Register();
}
