using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Desert.Walls;

public class SilkWall : ModWall, IPostWallFrame
{
	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = false;
		DustType = DustID.Silk;

		var entryColor = new Color(80, 80, 60);
		AddMapEntry(entryColor);
	}

	public void PostWallFrame(int i, int j, bool resetFrame)
	{
		var t = Main.tile[i, j];

		if (Main.rand.NextBool(15) && t.WallFrameX is 36 or 72 or 108 && t.WallFrameY is 36) //Plain center frames
		{
			Point result = new(324, 152);
			int random = Main.rand.Next(3);

			t.WallFrameX = result.X + 36 * random;
			t.WallFrameY = result.Y;
		}
	}
}