using SpiritReforged.Common.ItemCommon;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;

namespace SpiritReforged.Content.Ziggurat.Tiles.Furniture;

public class LapisSet : ILoadable
{
	public static Dictionary<string, int> TileTypes { get; } = [];

	public void Load(Mod mod) => ICreateItem.OnAutoloadItems += LoadLapisFurniture;

	private static void LoadLapisFurniture(Context context)
	{
		if (context == Context.After)
		{
			string saltName = typeof(LapisSet).Namespace + ".Lapis";
			TileHelper.ArgumentCollection arguments;

			LoadFurnitureSet(saltName, arguments = AllArgs(DustID.Cobalt, new(0.9f, 0.9f, 0.74f), distortGlow: false)
				- new BarrelTile()
				- new BenchTile()
				- new CandleTile()
				- new LanternTile(), 
				AutoContent.ItemType<CarvedLapis>()
			);

			foreach (FurnitureTile tile in arguments.Arguments)
				TileTypes.Add(tile.FurnitureName, tile.Type); //Collect the resulting types

			LapisCandle lapisCandle = ModContent.GetInstance<LapisCandle>();
			TileTypes.Add(lapisCandle.FurnitureName, lapisCandle.Type); //Manually include the candle as it's not added to arguments
		}
	}

	public void Unload() { }
}

public class LapisCandle : CandleTile, ICreateItem
{
	public void AddItemRecipes(ModItem modItem) => DataStructures.Recipes[FurnitureName]?.Invoke(modItem, AutoContent.ItemType<CarvedLapis>());

	public override void SetStaticDefaults()
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
		AddMapEntry(MapColor, Language.GetText("ItemName.Candle"));

		AdjTiles = [TileID.Candles];
		DustType = -1;

		base.SetStaticDefaults();
	}
}