using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;

namespace SpiritReforged.Content.Savanna.Tiles.Furniture;

public class DrywoodSet : ILoadable
{
	public void Load(Mod mod) => ILoadItem.PostAutoloadItems += LoadDrywoodFurniture;

	private static void LoadDrywoodFurniture()
	{
		string name = typeof(DrywoodSet).Namespace + ".Drywood";
		var light = Color.Orange.ToVector3();

		TileHelper.ArgumentCollection arguments = AllArgs(DustID.Pearlwood, new(light, true))
			- new BarrelTile()
			- new BenchTile()
			- new ChairTile();

		arguments.Get<ChandelierTile>().Settings = new(light, false);

		LanternTile lanternTile = arguments.Get<LanternTile>();
		lanternTile.WindCycle = 0;
		lanternTile.Settings = new(light, false);

		LoadFurnitureSet(name, arguments, AutoContent.ItemType<Drywood>());
	}

	public void Unload() { }
}

public class DrywoodChair : ChairTile, ILoadItem
{
	public void AddItemRecipes(ModItem modItem) => DataStructures.Recipes[FurnitureName]?.Invoke(modItem, AutoContent.ItemType<Drywood>());

	public override void SetStaticDefaults()
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

		AdjTiles = [TileID.Chairs];
		DustType = -1;

		base.SetStaticDefaults();
	}
}