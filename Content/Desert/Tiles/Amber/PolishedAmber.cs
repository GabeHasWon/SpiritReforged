using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

/// <summary> A placeable amber tile that also generates naturally. </summary>
public class PolishedAmber : ShiningAmber, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(10).AddIngredient(ItemID.Amber).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(ItemID.Amber).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		this.AutoItem().ResearchUnlockCount = 100;
	}
}