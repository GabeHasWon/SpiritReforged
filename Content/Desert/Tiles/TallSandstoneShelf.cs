using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Desert.Tiles;

public class TallSandstoneShelf : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolidTop[Type] = true;
		Main.tileSolid[Type] = true;
		Main.tileTable[Type] = true;
		SpiritSets.FrameHeight[Type] = 18;

		AddMapEntry(FurnitureTile.CommonColor);

		DustType = DustID.Dirt;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		const int variantWidth = 18 * 4;

		short frameX = 18 * 2;
		short frameY = 18;

		Tile tile = Main.tile[i, j];
		Tile left = Framing.GetTileSafely(i - 1, j);
		Tile right = Framing.GetTileSafely(i + 1, j);
		Tile top = Framing.GetTileSafely(i, j - 1);
		Tile bottom = Framing.GetTileSafely(i, j + 1);

		if (!top.HasTileType(Type) && !bottom.HasTileType(Type) && !left.HasTileType(Type) && !right.HasTileType(Type))
		{
			frameX = 0;
			frameY = 18 * 3;
		}
		else if (!top.HasTileType(Type) && !bottom.HasTileType(Type))
		{
			frameY += 18 * 2;
		}
		else if (!left.HasTileType(Type) && !right.HasTileType(Type))
		{
			frameX -= 18 * 2;
		}

		if (left.HasTileType(Type))
			frameX += 18;

		if (right.HasTileType(Type))
			frameX -= 18;

		if (!top.HasTileType(Type))
			frameY -= 18;

		if (!bottom.HasTileType(Type))
			frameY += 18;

		int randomFrame;
		if (resetFrame)
		{
			randomFrame = WorldGen.genRand.Next(3);
			tile.Get<TileWallWireStateData>().TileFrameNumber = randomFrame;
		}
		else
		{
			randomFrame = tile.TileFrameNumber;
		}

		frameX += (short)(variantWidth * randomFrame);

		tile.TileFrameX = frameX;
		tile.TileFrameY = frameY;

		return false;
	}
}