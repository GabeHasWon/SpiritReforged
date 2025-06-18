using RubbleAutoloader;
using SpiritReforged.Common;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Content.Savanna.Items;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public abstract class SavannaShrubsBase : ModTile
{
	protected virtual int[] Anchors => [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassMowed>(), ModContent.TileType<SavannaDirt>(), TileID.Sand];

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		SpiritSets.ConvertsByAdjacent[Type] = true;
		TileID.Sets.BreakableWhenPlacing[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		const int height = 44;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 56;
		TileObjectData.newTile.CoordinateHeights = [height];
		TileObjectData.newTile.DrawYOffset = -(height - 18 - 4); //4 pixels are reserved for the tile space below
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = Anchors;
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
		if (!Autoloader.IsRubble(Type))
		{
			ConversionHelper.Simple(i, j, conversionType,
				ModContent.TileType<SavannaShrubsCorrupt>(),
				ModContent.TileType<SavannaShrubsCrimson>(),
				ModContent.TileType<SavannaShrubsHallow>(),
				ModContent.TileType<SavannaShrubs>());
		}
	}
}

public class SavannaShrubs : SavannaShrubsBase, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<SavannaGrassSeeds>(), IAutoloadRubble.RubbleSize.Small);

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
		TileObjectData.newTile.AnchorValidTiles = Anchors;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 11;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(50, 92, 19));
		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
}

public class SavannaShrubsCorrupt : SavannaShrubsBase
{
	protected override int[] Anchors => [ModContent.TileType<SavannaGrassCorrupt>(), TileID.Ebonsand];
}

public class SavannaShrubsCrimson : SavannaShrubsBase
{
	protected override int[] Anchors => [ModContent.TileType<SavannaGrassCrimson>(), TileID.Crimsand];
}

public class SavannaShrubsHallow : SavannaShrubsBase
{
	protected override int[] Anchors => [ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaGrassHallowMowed>(), TileID.Pearlsand];
}