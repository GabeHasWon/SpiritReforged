using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooBench : SofaTile
{
	public override int CoreMaterial => AutoContent.ItemType<StrippedBamboo>();
}