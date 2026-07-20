using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;
using TileHelper.Common;
using TileHelper.Content.Tiles;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooBarrel : BarrelTile, ILoadItem
{
	public void AddItemRecipes(ModItem modItem) => DataStructures.Recipes[FurnitureName]?.Invoke(modItem, AutoContent.ItemType<StrippedBamboo>());

	public override void SetStaticDefaults()
	{
		Main.tileSpelunker[Type] = true;
		Main.tileContainer[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileOreFinderPriority[Type] = 500;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.BasicChest[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.IsAContainer[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
		TileObjectData.newTile.AnchorInvalidTiles = [127];
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		AddMapEntry(MapColor, MapEntry);

		AdjTiles = [TileID.Containers];
		DustType = DustID.PalmWood;
	}
}