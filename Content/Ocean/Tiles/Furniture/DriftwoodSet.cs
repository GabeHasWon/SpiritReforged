using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;

namespace SpiritReforged.Content.Ocean.Tiles.Furniture;

public class DriftwoodSet : ModSystem
{
	public override void Load() => ILoadItem.PostAutoloadItems += LoadDriftwoodFurniture;

	private static void LoadDriftwoodFurniture()
	{
		string saltName = typeof(DriftwoodSet).Namespace + ".Driftwood";
		LoadFurnitureSet(saltName, AllArgs(DustID.t_BorealWood, Color.Orange.ToVector3())
			- new ChestTile()
			- new SofaTile(),
			AutoContent.ItemType<Driftwood>()
		);
	}

	public override void PostSetupContent() => SpiritClassic.AddItemReplacement("DriftwoodWorkbenchItem", SpiritReforgedMod.Instance.Find<ModItem>("DriftwoodWorkBenchItem").Type);
}