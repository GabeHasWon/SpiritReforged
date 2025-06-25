using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Savanna.Items;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassTile Glow")]
public class StargrassMowed : StargrassTile
{
	public override ConversionHandler.Set ConversionSet => new()
	{
		{ BiomeConversionID.Corruption, TileID.CorruptGrass },
		{ BiomeConversionID.Crimson, TileID.CrimsonGrass },
		{ BiomeConversionID.Hallow, TileID.GolfGrassHallowed },
		{ BiomeConversionID.PurificationPowder, TileID.GolfGrass },
		{ SavannaConversion.ConversionType, ModContent.TileType<SavannaGrass>() }
	};

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileMaterials.SetForTileId(Type, TileMaterials.GetByTileId(TileID.GolfGrass));
	}

	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHandler.FindSet(nameof(StargrassMowed), conversionType, out int newType))
			WorldGen.ConvertTile(i, j, newType);
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (!WorldGen.TileIsExposedToAir(i, j))
			Main.tile[i, j].TileType = TileID.GolfGrass;

		return true;
	}

	public override void GrowPlants(int i, int j) { }
}