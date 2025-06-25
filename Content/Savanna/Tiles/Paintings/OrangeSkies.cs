using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.Paintings;

public class OrangeSkies : PaintingTile
{
	public override int TileHeight => 3;
	public override int TileWidth => 2;

	public override void AddItemRecipes(ModItem item)
	{
		if (CrossMod.Thorium.Enabled && CrossMod.Thorium.TryFind("BlankPainting", out ModItem canvas))
			item.CreateRecipe().AddIngredient(canvas.Type).AddIngredient(AutoContent.ItemType<SavannaTorch>(), 5).AddTile(TileID.WorkBenches).Register();
	}
}
