using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Ocean.Tiles.Furniture;

public class DriftwoodDoor : DoorTile
{
	public override int CoreMaterial => AutoContent.ItemType<Driftwood>();
}
