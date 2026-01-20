using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Cartography.Maps;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class AncientBooks : ModTile, IAutoloadTileItem
{
	public void StaticItemDefaults()
	{
		ItemLootDatabase.AddItemRule(ItemID.OasisCrate, ItemDropRule.Common(Type, 2, 3, 5));
		ItemLootDatabase.AddItemRule(ItemID.OasisCrateHard, ItemDropRule.Common(Type, 3, 5));

		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.Book;
		ItemID.Sets.ShimmerTransformToItem[ItemID.Book] = Type;
	}

	public override void SetStaticDefaults()
	{
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 4;
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