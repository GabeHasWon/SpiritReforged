using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Desert.Tiles;

public class BronzePlating : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(20).AddRecipeGroup("CopperBars").AddTile(TileID.Anvils).Register();
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(new Color(200, 74, 48));

		DustType = DustID.Copper;
		this.AutoItem().ResearchUnlockCount = 100;
	}
}