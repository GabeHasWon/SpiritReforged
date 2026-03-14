using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.Paintings;

public class StrongWinds : PaintingTile
{
	public override Point TileSize => new(3, 3);

	public override void AddItemRecipes(ModItem item)
	{
		if (CrossMod.Thorium.Enabled && CrossMod.Thorium.TryFind("BlankPainting", out ModItem canvas))
			item.CreateRecipe().AddIngredient(canvas.Type).AddIngredient(AutoContent.ItemType<Drywood>(), 5).AddTile(TileID.WorkBenches).Register();
	}
}