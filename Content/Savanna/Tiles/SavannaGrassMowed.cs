using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.Corruption;
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
		RegisterItemDrop(ItemMethods.AutoItemType<SavannaDirt>());
		AddMapEntry(MapColor);
	}

	public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => tileTypeBeingPlaced != ItemMethods.AutoItemType<SavannaDirt>();
	public override void Convert(int i, int j, int conversionType)
	{
		int type = (ConversionType)conversionType switch
		{
			ConversionType.Hallow => ModContent.TileType<SavannaGrassHallowMowed>(),
			ConversionType.Corrupt => ModContent.TileType<SavannaGrassCorrupt>(),
			ConversionType.Crimson => ModContent.TileType<SavannaGrassCrimson>(),
			_ => ModContent.TileType<SavannaGrassMowed>()
		};

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