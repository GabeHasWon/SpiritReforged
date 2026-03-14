using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class PaintingTile : FurnitureTile
{
	public virtual Point TileSize => new(2, 2);

	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.buyPrice(gold: 2);

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.FramesOnKillWall[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = TileSize.Y;
		TileObjectData.newTile.Width = TileSize.X;
		TileObjectData.newTile.CoordinateHeights = [.. Enumerable.Repeat(16, TileSize.Y)];
		TileObjectData.newTile.Origin = new(TileSize.X - 2, TileSize.Y - 2);

		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = AnchorData.Empty;
		TileObjectData.newTile.AnchorWall = true;
		TileObjectData.addTile(Type);

		DustType = DustID.WoodFurniture;
		AddMapEntry(new Color(23, 23, 23), Language.GetText("MapObject.Painting"));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 3 : 10;
}