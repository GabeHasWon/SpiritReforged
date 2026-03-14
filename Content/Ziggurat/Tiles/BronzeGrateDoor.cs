using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class BronzeGrateDoor : DoorTile
{
	public override IFurnitureData Info => new BasicInfo(this.AutoModItem(), AutoContent.ItemType<BronzePlating>());
}