using Terraria.DataStructures;

namespace SpiritReforged.Content.Crossmod.SpookyForest;

internal class StarGourdGreen : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdGreen>("GourdBlockGreenItem");
}

internal class StarGourdLime : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdLime>("GourdBlockLimeItem");

	protected override bool ModifyObjectData(TileObjectData newTile)
	{
		newTile.Width = 3;
		newTile.Height = 3;
		newTile.CoordinateHeights = [16, 16, 16];
		newTile.Origin = new Point16(1, 2);
		newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
		newTile.CoordinateWidth = 16;
		newTile.CoordinatePadding = 2;
		newTile.DrawYOffset = 2;

		AddMapEntry(new Color(194, 218, 9));
		return false;
	}
}

internal class StarGourdOrangeLime : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdOrangeLime>("GourdBlockLimeOrangeItem");

	protected override bool ModifyObjectData(TileObjectData newTile)
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.DrawYOffset = 2;

		AddMapEntry(new Color(107, 171, 15));
		return false;
	}
}
