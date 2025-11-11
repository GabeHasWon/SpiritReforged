using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Desert.Tiles;

public class AncientBanner : ModTile, ISwayTile, IAutoloadTileItem
{
	public int Style => (int)TileDrawing.TileCounterType.MultiTileVine;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.Silk, 3).AddTile(TileID.Loom).Register();

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = Point16.Zero;
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 2, 0);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(150, 80, 60), Language.GetText("ItemName.Banners"));
		RegisterItemDrop(this.AutoItemType());
		DustType = -1;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}