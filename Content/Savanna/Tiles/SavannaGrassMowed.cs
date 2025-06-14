using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaGrassMowed : GrassTile
{
	protected override int DirtType => ModContent.TileType<SavannaDirt>();
	protected virtual Color MapColor => new(104, 156, 70);

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
		int type = conversionType switch
		{
			BiomeConversionID.Corruption => ModContent.TileType<SavannaGrassCorrupt>(),
			BiomeConversionID.Crimson => ModContent.TileType<SavannaGrassCrimson>(),
			BiomeConversionID.Hallow => ModContent.TileType<SavannaGrassHallowMowed>(),
			_ => ConversionCalls.GetConversionType(conversionType, Type, ModContent.TileType<SavannaGrassMowed>()),
		};

		if (type != -1)
			WorldGen.ConvertTile(i, j, type);
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
}