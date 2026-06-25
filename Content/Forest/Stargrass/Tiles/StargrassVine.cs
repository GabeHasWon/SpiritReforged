using SpiritReforged.Common.TileCommon.PresetTiles;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

public class StargrassVine : VineTile
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>()];
		Sets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, StargrassTile.GetGlowColor);

		AddMapEntry(new Color(24, 135, 28));
	}
}