using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class AncientBanner : ModTile, ISwayTile, IAutoloadTileItem
{
	public int Style => (int)TileDrawing.TileCounterType.MultiTileVine;

	public void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(silver: 2);
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
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.Platform, TileObjectData.newTile.Width, 0);
		TileObjectData.newAlternate.DrawYOffset = -8;

		TileObjectData.addAlternate(0);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(70, 70, 150), Language.GetText("MapObject.Banner"));
		RegisterItemDrop(this.AutoItemType());
		DustType = -1;
	}
}