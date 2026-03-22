using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class SandySandstone : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileMerge[TileID.Sand][Type] = true;

		TileID.Sets.ChecksForMerge[Type] = true;

		this.Merge(TileID.Sandstone, ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<CrackedSandstone>());
		AddMapEntry(new Color(174, 74, 48));
		RegisterItemDrop(AutoContent.ItemType<CrackedSandstone>());

		DustType = DustID.Sand;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, TileID.Sand, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		Tile tile = Main.tile[i, j];
		Tile leftTile = Framing.GetTileSafely(i - 1, j);
		Tile rightTile = Framing.GetTileSafely(i + 1, j);
		Tile upTile = Framing.GetTileSafely(i, j - 1);

		if (tile.TileFrameX > 0 && tile.TileFrameX < 72 && tile.TileFrameY == 36 && upTile.HasTile && upTile.TileType == TileID.Sand) //Fix niche sand merge
		{
			tile.TileFrameX += 216;
			tile.TileFrameY = 18;
		}

		if (tile.TileFrameX > 0 && tile.TileFrameX < 72 && tile.TileFrameY == 18) //Corners
		{
			if (leftTile.HasTile && leftTile.TileType == Type && rightTile.HasTile && rightTile.TileType == Type && upLeft != Type && upRight != Type) //Right and left corners
			{
				tile.TileFrameX = 162;
				tile.TileFrameY = 90;
			}
			else if (leftTile.HasTile && leftTile.TileType == Type && upLeft != Type) //Left corner
			{
				tile.TileFrameX = 18;
				tile.TileFrameY = 108;
			}
			else if (rightTile.HasTile && rightTile.TileType == Type && upRight != Type) //Right corner
			{
				tile.TileFrameX = 0;
				tile.TileFrameY = 108;
			}
		}
	}
}