using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class StrippedBamboo : ModTile, IAutoloadTileItem
{
	public void SetItemDefaults(ModItem item) => item.Item.value = 1;
	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.BambooBlock).AddTile(TileID.WorkBenches).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;

		Main.tileMerge[Type][TileID.WoodBlock] = true;
		Main.tileMerge[TileID.WoodBlock][Type] = true;

		Main.tileMerge[Type][TileID.Sand] = true;
		Main.tileMerge[TileID.Sand][Type] = true;

		DustType = DustID.PalmWood;
		AddMapEntry(new Color(145, 128, 109));

		this.AutoItem().ResearchUnlockCount = 100;
		SpiritClassic.AddItemReplacement("StrippedBamboo", this.AutoItem().type);
	}
}