using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Replacement;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;

namespace SpiritReforged.Content.Ocean.Tiles.Furniture;

public class DriftwoodSet : ModSystem
{
	public override void Load() => ILoadItem.PostAutoloadItems += LoadDriftwoodFurniture;

	private static void LoadDriftwoodFurniture() => LoadFurnitureSet(typeof(DriftwoodSet).Namespace + ".Driftwood", AllArgs(DustID.t_BorealWood, new(Color.Orange.ToVector3(), true))
		- nameof(ChestTile)
		- nameof(SofaTile),
		AutoContent.ItemType<Driftwood>()
	);

	public override void PostSetupContent() => ReplacementSystem.AddItemReplacement("SpiritMod/DriftwoodWorkbenchItem", SpiritReforgedMod.Instance.Find<ModItem>("DriftwoodWorkBenchItem").Type);
}