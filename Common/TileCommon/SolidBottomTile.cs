namespace SpiritReforged.Common.TileCommon;

internal class SolidBottomTile : GlobalTile
{
	internal static HashSet<int> TileTypes = []; //Strictly for visual merging

	public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		if (Main.tileSolid[type] && Main.tile[i, j - 1].HasTile && TileTypes.Contains(Main.tile[i, j - 1].TileType))
		{
			var t = Main.tile[i, j];
			if (t.IsHalfBlock || t.Slope != SlopeType.Solid)
				return true;

			if (!WorldGen.SolidOrSlopedTile(i - 1, j) && !WorldGen.SolidOrSlopedTile(i + 1, j)) //empty-empty
			{
				t.TileFrameX = 18 * 5;
				t.TileFrameY = 0;
			}
			else if (!WorldGen.SolidOrSlopedTile(i - 1, j)) //empty-solid
			{
				t.TileFrameX = 0;
				t.TileFrameY = 0;
			}
			else if (!WorldGen.SolidOrSlopedTile(i + 1, j)) //solid-empty
			{
				t.TileFrameX = 18 * 4;
				t.TileFrameY = 0;
			}
			else //solid-solid
			{
				t.TileFrameX = 18;
				t.TileFrameY = 18;
			}

			return false;
		}

		return true;
	}
}
