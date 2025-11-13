using SpiritReforged.Content.Forest.Cartography.Maps;

namespace SpiritReforged.Content.Desert.Tiles;

public class AncientBooks : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<TornMapPiece>(), 4);
		DustType = DustID.Dirt;
	}
}