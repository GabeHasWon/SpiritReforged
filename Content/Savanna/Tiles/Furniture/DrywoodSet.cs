using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles.Furniture;

public class DrywoodSet : FurnitureSet
{
	public override string Name => "Drywood";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<Drywood>(), Color.Orange.ToVector3() / 255f, DustID.t_PearlWood);

	public override void OnPostSetupContent()
	{
		var mod = SpiritReforgedMod.Instance;
		SpiritSets.Workbench[mod.Find<ModItem>("DrywoodWorkBenchItem").Type] = true;
	}
}

public class DrywoodChair : ChairTile
{
	public override IFurnitureData Info => new BasicInfo(this.AutoModItem(), AutoContent.ItemType<Drywood>());
	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.CanBeSatOnForNPCs[Type] = true;
		TileID.Sets.CanBeSatOnForPlayers[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateWidth = 18;
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("ItemName.Chair"));
		AdjTiles = [TileID.Chairs];
		DustType = -1;
	}
}