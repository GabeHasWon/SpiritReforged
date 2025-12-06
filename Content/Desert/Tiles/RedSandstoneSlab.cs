using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;

namespace SpiritReforged.Content.Desert.Tiles;

public class RedSandstoneSlab : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), TileID.Sand);
		AddMapEntry(new Color(174, 74, 48));

		DustType = DustID.DynastyShingle_Red;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand);
	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight) => RuinedSandstonePillar.SetupMerge(Type, ref up, ref down);

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		const short rowHeight = 90;

		Tile tile = Main.tile[i, j];

		if (tile.IsHalfBlock || tile.Slope != SlopeType.Solid)
			return;

		int frameX = tile.TileFrameX;
		int frameY = tile.TileFrameY % rowHeight;
		int section = (j + (i + 1) / 2) % 3;

		//Horizontal random styles
		if ((frameX == 18 || frameX == 18 * 2 || frameX == 18 * 3) && (frameY == 0 || frameY == 18 || frameY == 18 * 2))
			tile.TileFrameX = (short)(18 * (1 + section));

		//Horizontal random styles for corners
		if (frameY > 18 * 2 && frameY < 18 * 5 && frameX < 18 * 6)
			tile.TileFrameX = (short)(tile.TileFrameX % (18 * 2) + 18 * 2 * section);

		//Vertical random styles
		if ((frameX == 0 || frameX == 18 * 4) && (frameY == 0 || frameY == 18 || frameY == 18 * 2))
			tile.TileFrameY = (short)(18 * section);

		if (i % 2 == 0)
			tile.TileFrameY += 90;
	}
}