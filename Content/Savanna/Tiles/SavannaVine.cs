using SpiritReforged.Common;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using static Terraria.GameContent.Drawing.TileDrawing;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaVine : ModTile, ISwayTile
{
	public int Style => (int)TileCounterType.Vine;

	public override void SetStaticDefaults()
	{
		Main.tileBlockLight[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileLavaDeath[Type] = true;

		SpiritSets.ConvertsByAdjacent[Type] = true;
		TileID.Sets.IsVine[Type] = true;
		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorAlternateTiles = [Type];

		PreAddObjectData();
		TileObjectData.addTile(Type);

		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;
	}

	public virtual void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>()];
		AddMapEntry(new Color(24, 135, 28));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void Convert(int i, int j, int conversionType)
	{
		if (!ConvertAdjacentSet.Converting)
			return;

		int type = conversionType switch
		{
			BiomeConversionID.Corruption => ModContent.TileType<SavannaVineCorrupt>(),
			BiomeConversionID.Crimson => ModContent.TileType<SavannaVineCrimson>(),
			BiomeConversionID.Hallow => ModContent.TileType<SavannaVineHallow>(),
			_ => ConversionCalls.GetConversionType(conversionType, Type, ModContent.TileType<SavannaVine>()),
		};

		if (type != -1 && ConvertAdjacentSet.CheckAnchors(i, j, type))
			WorldGen.ConvertTile(i, j, type);
	}
}

public class SavannaVineCorrupt : SavannaVine
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorAlternateTiles = [ModContent.TileType<SavannaGrassCorrupt>()];

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
		TileObjectData.newTile.AnchorAlternateTiles = [ModContent.TileType<SavannaGrassCrimson>()];

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
		TileObjectData.newTile.AnchorAlternateTiles = [ModContent.TileType<SavannaGrassHallow>()];

		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 1;

		AddMapEntry(new(78, 193, 227));
		DustType = DustID.HallowedPlants;
	}
}