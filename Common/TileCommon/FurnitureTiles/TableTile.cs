using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.FurnitureTiles;

public abstract class TableTile : FurnitureTile
{
	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileSolidTop[Type] = true;
		Main.tileTable[Type] = true;
		Main.tileNoAttach[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.IgnoredByNpcStepUp[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidWithTop | AnchorType.SolidTile, 3, 0);
		TileObjectData.newTile.Origin = new Point16(2, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("MapObject.Table"));
		AdjTiles = [TileID.Tables];
		DustType = -1;
	}
}
