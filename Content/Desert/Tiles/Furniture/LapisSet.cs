using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Desert.Tiles.Furniture;

public class LapisSet : FurnitureSet
{
	public override string Name => "Lapis";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<CarvedLapis>(), new(0.9f, 0.9f, 0.74f), DustID.Cobalt);
	public override bool Autoload(FurnitureTile tile) => Excluding(tile, Types.Barrel, Types.Bench, Types.Candle);
}

public class LapisCandle : CandleTile
{
	public override IFurnitureData Info => ModContent.GetInstance<LapisSet>().GetInfo(this);
	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
		TileObjectData.newTile.CoordinateHeights = [20];
		TileObjectData.newTile.DrawYOffset = -4;
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(CommonColor, Language.GetText("ItemName.Candle"));
		AdjTiles = [TileID.Candles];
		DustType = -1;
	}
}