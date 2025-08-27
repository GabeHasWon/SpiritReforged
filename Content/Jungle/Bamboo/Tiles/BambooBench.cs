using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooBench : SofaTile
{
	public override IFurnitureData Info => new BasicInfo(this.AutoModItem(), AutoContent.ItemType<StrippedBamboo>());
}