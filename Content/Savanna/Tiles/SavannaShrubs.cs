using RubbleAutoloader;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Content.Savanna.Items;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public abstract class SavannaShrubsBase : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		const int height = 44;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 56;
		TileObjectData.newTile.CoordinateHeights = [height];
		TileObjectData.newTile.DrawYOffset = -(height - 18 - 4); //4 pixels are reserved for the tile space below
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassMowed>(), 
			ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>(), 
			ModContent.TileType<SavannaDirt>(), TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 11;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(104, 156, 7));
		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
	public override void Convert(int i, int j, int conversionType)
	{
		if (Autoloader.IsRubble(Type))
			return;

		if (ConversionHelper.FindType(conversionType, Main.tile[i, j].TileType, ModContent.TileType<SavannaShrubsCorrupt>(), ModContent.TileType<SavannaShrubsCrimson>(), ModContent.TileType<SavannaShrubsHallow>(), ModContent.TileType<SavannaShrubs>()) is int value && value != -1)
			WorldGen.ConvertTile(i, j, value);
	}
}

public class SavannaShrubs : SavannaShrubsBase, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<SavannaGrassSeeds>(), IAutoloadRubble.RubbleSize.Small);
}

public class SavannaShrubsCorrupt : SavannaShrubsBase;

public class SavannaShrubsCrimson : SavannaShrubsBase;

public class SavannaShrubsHallow : SavannaShrubsBase;