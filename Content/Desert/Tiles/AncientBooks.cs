using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Cartography.Maps;

namespace SpiritReforged.Content.Desert.Tiles;

public class AncientBooks : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;
		TileObjectData.newTile.CoordinateHeights = [18];
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<TornMapPiece>(), 4);
		RegisterItemDrop(this.AutoItemType(), 0, 1, 2, 3);
		DustType = DustID.Dirt;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = Main.tile[i, j].TileFrameX == 18 * 4 ? ModContent.ItemType<TornMapPiece>() : this.AutoItemType();
	}
}