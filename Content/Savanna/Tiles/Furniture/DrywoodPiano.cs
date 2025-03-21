using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.Furniture;

public class DrywoodPiano : PianoTile
{
	public override int CoreMaterial => ItemMethods.AutoItemType<Drywood>();
}
