using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Savanna.Items;

namespace SpiritReforged.Content.Savanna.Tiles.Paintings;

public class DustyFields : PaintingTile
{
	public override Point TileSize => new(3, 3);

	public override void AddItemRecipes(ModItem item) 
	{
		if (CrossMod.Thorium.Enabled && CrossMod.Thorium.TryFind("BlankPainting", out ModItem canvas))
			item.CreateRecipe().AddIngredient(canvas.Type).AddIngredient(ModContent.ItemType<SavannaGrassSeeds>(), 5).AddTile(TileID.WorkBenches).Register();
	}
}