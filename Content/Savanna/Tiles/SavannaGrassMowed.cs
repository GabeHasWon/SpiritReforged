using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaGrassMowed : GrassTile, ISetConversion
{
	protected override int DirtType => ModContent.TileType<SavannaDirt>();
	protected virtual Color MapColor => new(104, 156, 70);

	public ConversionHandler.Set ConversionSet => ConversionHelper.CreateSimple(ModContent.TileType<SavannaGrassCorrupt>(), 
		ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallowMowed>(), ModContent.TileType<SavannaGrassMowed>(), false);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileMaterials.SetForTileId(Type, TileMaterials.GetByTileId(TileID.GolfGrass));
		RegisterItemDrop(AutoContent.ItemType<SavannaDirt>());
		AddMapEntry(MapColor);
	}

	public override void RandomUpdate(int i, int j) { }
	public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => tileTypeBeingPlaced != AutoContent.ItemType<SavannaDirt>();

	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHandler.FindSet(nameof(SavannaGrassMowed), conversionType, out int newType))
			WorldGen.ConvertTile(i, j, newType);
	}
}

public class SavannaGrassHallowMowed : SavannaGrassMowed
{
	protected override Color MapColor => new(78, 193, 227);
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 20;
	}

	public override void RandomUpdate(int i, int j) => WorldGen.SpreadInfectionToNearbyTile(i, j, BiomeConversionID.Hallow);
}