namespace SpiritReforged.Content.Ocean.Items.Driftwood;

public class DriftwoodTable : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolidTop[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileTable[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
		TileObjectData.addTile(Type);
		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
		AddMapEntry(new Color(165, 150, 0), Language.GetText("MapObject.Table"));
		TileID.Sets.DisableSmartCursor[Type] = true;
		DustType = -1;
	}
}