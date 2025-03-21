using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Ocean.Tiles.Furniture;

public class DriftwoodPiano : PianoTile
{
	public override int CoreMaterial => ItemMethods.AutoItemType<Driftwood>();
}
