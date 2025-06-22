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

		TileID.Sets.IsVine[Type] = true;
		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

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

	public virtual void PreAddObjectData() => AddMapEntry(new Color(24, 135, 28));
	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void Convert(int i, int j, int conversionType)
	{
		int type = Main.tile[i, j].TileType;

		if (Framing.GetTileSafely(i, j - 1).TileType == type)
			return; //Return if this is not the base of the vine

		if (ConversionHelper.FindType(conversionType, type, ModContent.TileType<SavannaVineCorrupt>(), ModContent.TileType<SavannaVineCrimson>(), ModContent.TileType<SavannaVineHallow>(), ModContent.TileType<SavannaVine>()) is int value && value != -1)
		{
			int bottom = j;
			while (WorldGen.InWorld(i, bottom, 2) && Main.tile[i, bottom].TileType == type)
				bottom++; //Iterate to the bottom of the vine

			int height = bottom - j;
			ConversionHelper.ConvertTiles(i, j, 1, height, value);
		}
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