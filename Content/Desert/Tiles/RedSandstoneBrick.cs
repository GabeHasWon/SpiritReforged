using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;

namespace SpiritReforged.Content.Desert.Tiles;

public class RedSandstoneBrick : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), TileID.Sand);
		Main.tileMerge[Type][ModContent.TileType<RuinedSandstonePillar>()] = true;

		AddMapEntry(new Color(174, 74, 48));

		DustType = DustID.DynastyShingle_Red;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand);
}