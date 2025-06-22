using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassTile Glow")] //Use Stargrass' glow
public class StargrassVine : VineTile
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>(), TileID.CorruptGrass, TileID.CrimsonGrass, TileID.HallowedGrass, TileID.Grass];
		AddMapEntry(new Color(24, 135, 28));

		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void Convert(int i, int j, int conversionType)
	{
		//if (ConversionHelper.FindType(conversionType, Main.tile[i, j].TileType, TileID.CorruptVines, TileID.CrimsonVines, TileID.HallowedVines, TileID.Vines) is int value && value != -1)
		//	ConvertVines(i, j, value);
	}
}