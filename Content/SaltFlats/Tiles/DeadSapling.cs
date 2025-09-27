using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class DeadSapling : SaplingTile<DeadTree>
{
	public override int[] AnchorTypes => [ModContent.TileType<SaltBlockDull>()];
}