using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

public class StargrassSapling : SaplingTile
{
	public override void PreAddObjectData() => TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>()];
}