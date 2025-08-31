using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Ocean.Tiles.Furniture;

public class DriftwoodSet : FurnitureSet
{
	public override string Name => "Driftwood";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<Driftwood>(), Color.Orange.ToVector3() / 255f, DustID.t_BorealWood, true);

	public override void OnPostSetupContent()
	{
		SpiritClassic.AddItemReplacement("DriftwoodWorkbenchItem", SpiritReforgedMod.Instance.Find<ModItem>("DriftwoodWorkBenchItem").Type);
	}
}