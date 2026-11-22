using RubbleAutoloader;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Content.Savanna.Items;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public abstract class SavannaShrubsBase : ModTile, ISetConversion
{
	public ConversionHandler.Set ConversionSet => new()
	{
		{ ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaShrubsCorrupt>() },
		{ TileID.Ebonsand, ModContent.TileType<SavannaShrubsCorrupt>() },
		{ ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaShrubsCrimson>() },
		{ TileID.Crimsand, ModContent.TileType<SavannaShrubsCrimson>() },
		{ ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaShrubsHallow>() },
		{ ModContent.TileType<SavannaGrassHallowMowed>(), ModContent.TileType<SavannaShrubsHallow>() },
		{ TileID.Pearlsand, ModContent.TileType<SavannaShrubsHallow>() },
		{ ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaShrubs>() },
		{ ModContent.TileType<SavannaGrassMowed>(), ModContent.TileType<SavannaShrubs>() },
		{ TileID.Sand, ModContent.TileType<SavannaShrubs>() },
	};

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;

		const int height = 44;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 56;
		TileObjectData.newTile.CoordinateHeights = [height];
		TileObjectData.newTile.DrawYOffset = -(height - 18 - 4); //4 pixels are reserved for the tile space below
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 11;

		PreAddObjectData();

		TileObjectData.addTile(Type);
	}

	public virtual void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassMowed>(), ModContent.TileType<SavannaDirt>(), TileID.Sand];
		AddMapEntry(new Color(104, 156, 7));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (!Autoloader.IsRubble(Type))
			FrameConvert(i, j, Main.tile[i, j].TileType);

		return true;
	}

	public static void FrameConvert(int i, int j, int type)
	{
		if (ConversionHandler.FindSet(nameof(SavannaShrubs), Framing.GetTileSafely(i, j + 1).TileType, out int newType))
			WorldGen.ConvertTile(i, j, newType);
	}
}

public class SavannaShrubs : SavannaShrubsBase, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<SavannaGrassSeeds>(), IAutoloadRubble.RubbleSize.Small);
}

public class SavannaShrubsCorrupt : SavannaShrubsBase
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaDirt>(), TileID.Ebonsand];

		DustType = DustID.Corruption;
		AddMapEntry(new(109, 106, 174));
	}
}

public class SavannaShrubsCrimson : SavannaShrubsBase
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaDirt>(), TileID.Crimsand];

		DustType = DustID.CrimsonPlants;
		AddMapEntry(new(183, 69, 68));
	}
}

public class SavannaShrubsHallow : SavannaShrubsBase
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaGrassHallowMowed>(), ModContent.TileType<SavannaDirt>(), TileID.Pearlsand];

		DustType = DustID.HallowedPlants;
		AddMapEntry(new(78, 193, 227));
	}
}