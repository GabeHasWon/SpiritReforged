using TileHelper.Common;

namespace SpiritReforged.Content.Basalt.Tiles;

public class BasaltTile : ModTile, ILoadItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.ChecksForMerge[Type] = true;

		AddMapEntry(new Color(60, 45, 40));
		DustType = DustID.DarkCelestial;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, ModContent.TileType<AccentBasalt>(), ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}

public class AccentBasalt : ModTile, ILoadItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		Main.tileMerge[Type][ModContent.TileType<BasaltTile>()] = true;

		AddMapEntry(new Color(60, 45, 40));
		DustType = DustID.DarkCelestial;
	}
}