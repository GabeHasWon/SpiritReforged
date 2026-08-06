using SpiritReforged.Common.TileCommon;
using TileHelper.Common;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class BronzePlating : ModTile, ILoadItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(20).AddRecipeGroup("CopperBars").AddTile(TileID.Anvils).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>());
		AddMapEntry(new Color(200, 74, 48));

		DustType = DustID.Copper;
		HitSound = SoundID.Tink;
	}
}