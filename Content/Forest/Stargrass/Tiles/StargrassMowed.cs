using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.WorldGeneration;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassTile Glow")]
public class StargrassMowed : StargrassTile
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;
		TileID.Sets.Conversion.Grass[Type] = true;

		RegisterItemDrop(ItemID.DirtBlock);
		AddMapEntry(new Color(28, 216, 151));
		DustType = DustID.Flare_Blue;
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