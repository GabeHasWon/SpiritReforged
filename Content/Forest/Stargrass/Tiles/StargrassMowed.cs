using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.WorldGeneration;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassTile Glow")]
public class StargrassMowed : StargrassTile
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileMaterials.SetForTileId(Type, TileMaterials.GetByTileId(TileID.GolfGrass));
	}

	public override void Convert(int i, int j, int conversionType)
	{
		if (conversionType == BiomeConversionID.PurificationPowder)
			WorldGen.ConvertTile(i, j, TileID.GolfGrass);
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (OpenTools.GetOpenings(i, j, false, false, true) == OpenFlags.None) //Surrounded by solid tiles
			Main.tile[i, j].TileType = TileID.GolfGrass;

		return true;
	}

	public override void GrowPlants(int i, int j) { }
}