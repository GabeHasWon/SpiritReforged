using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.Furniture;

public class DrywoodChest : ChestTile
{
	public override int CoreMaterial => AutoContent.ItemType<Drywood>();
}
