using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.Furniture;

public class DrywoodWorkBench : WorkBenchTile
{
	public override int CoreMaterial => AutoContent.ItemType<Drywood>();
}
