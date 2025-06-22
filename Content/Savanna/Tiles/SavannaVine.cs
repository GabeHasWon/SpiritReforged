using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaVine : VineTile
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>()];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];

		PreAddObjectData();

		TileObjectData.addTile(Type);

		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;
	}

	public override void PreAddObjectData() => AddMapEntry(new Color(24, 135, 28));

	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHelper.FindType(conversionType, Main.tile[i, j].TileType, ModContent.TileType<SavannaVineCorrupt>(), ModContent.TileType<SavannaVineCrimson>(), ModContent.TileType<SavannaVineHallow>(), ModContent.TileType<SavannaVine>()) is int value && value != -1)
			ConvertVines(i, j, value);
	}
}

public class SavannaVineCorrupt : SavannaVine
{
	public override void PreAddObjectData()
	{
		TileID.Sets.AddCorruptionTile(Type);
		TileID.Sets.Corrupt[Type] = true;

		AddMapEntry(new(109, 106, 174));
		DustType = DustID.Corruption;
	}
}

public class SavannaVineCrimson : SavannaVine
{
	public override void PreAddObjectData()
	{
		TileID.Sets.AddCrimsonTile(Type);
		TileID.Sets.Crimson[Type] = true;

		AddMapEntry(new(183, 69, 68));
		DustType = DustID.CrimsonPlants;
	}
}

public class SavannaVineHallow : SavannaVine
{
	public override void PreAddObjectData()
	{
		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 1;

		AddMapEntry(new(78, 193, 227));
		DustType = DustID.HallowedPlants;
	}
}