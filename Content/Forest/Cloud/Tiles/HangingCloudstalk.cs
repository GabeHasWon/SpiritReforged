using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using static Terraria.GameContent.Drawing.TileDrawing;

namespace SpiritReforged.Content.Forest.Cloud.Tiles;

public class HangingCloudstalk : ModTile, ISwayTile, IAutoloadTileItem
{
	public void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(0, 0, 1, 50);
	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.PotSuspended)
		.AddIngredient(ModContent.ItemType<Items.Cloudstalk>()).Register();

	public int Style => (int)TileCounterType.MultiTileVine;

	public override void SetStaticDefaults()
	{
		Main.tileSolidTop[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.DrawYOffset = -2;
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorTop = new AnchorData(TileObjectData.newTile.AnchorTop.type, 2, 0);
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(28, 138, 72));
		RegisterItemDrop(this.AutoItem().type);

		DustType = -1;

		SpiritClassic.AddItemReplacement("HangingCloudstalk", this.AutoItem().type);
	}
}
