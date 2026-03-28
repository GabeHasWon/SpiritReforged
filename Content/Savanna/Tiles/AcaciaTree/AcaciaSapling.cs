using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

public class AcaciaSapling : SaplingTile<AcaciaTree>
{
	public override int[] AnchorTypes => [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassMowed>()];
}

public class AcaciaSaplingCorrupt : SaplingTile<AcaciaTreeCorrupt>
{
	public override int[] AnchorTypes => [ModContent.TileType<SavannaGrassCorrupt>()];
}

public class AcaciaSaplingCrimson : SaplingTile<AcaciaTreeCrimson>
{
	public override int[] AnchorTypes => [ModContent.TileType<SavannaGrassCrimson>()];
}

public class AcaciaSaplingHallow : SaplingTile<AcaciaTreeHallow>
{
	public override int[] AnchorTypes => [ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaGrassHallowMowed>()];
}