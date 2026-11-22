using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Ziggurat.Walls;

public class SandyZigguratWall : ModWall, IPostWallFrame
{
	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = false;
		Main.wallBlend[Type] = ModContent.WallType<RedSandstoneBrickWall>();

		DustType = DustID.DynastyShingle_Red;
		AddMapEntry(new(150, 70, 40));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public void PostWallFrame(int i, int j, bool resetFrame)
	{
		Tile below = Framing.GetTileSafely(i, j + 1);

		if (below.WallType == RedSandstoneBrickWall.UnsafeType)
		{
			Tile left = Framing.GetTileSafely(i - 1, j);
			Tile right = Framing.GetTileSafely(i + 1, j);
			Tile up = Framing.GetTileSafely(i, j - 1);

			if (left.WallType != WallID.None && right.WallType != WallID.None && up.WallType != WallID.None) //Check if encased
			{
				Tile tile = Main.tile[i, j];
				Point result = new(324, 152);

				tile.WallFrameX = result.X + 36 * tile.Get<TileWallWireStateData>().WallFrameNumber;
				tile.WallFrameY = result.Y;
			}
		}
	}
}