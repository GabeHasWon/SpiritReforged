using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

public class AcaciaRootsSmall : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = Point16.Zero;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassMowed>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaDirt>()];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.RandomStyleRange = 2;
		TileObjectData.addTile(Type);

		DustType = DustID.WoodFurniture;
		RegisterItemDrop(AutoContent.ItemType<Drywood>());
		AddMapEntry(new Color(87, 61, 51));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;
	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHelper.FindType(conversionType, Main.tile[i, j].TileType, ModContent.TileType<AcaciaRootsSmallCorrupt>(), ModContent.TileType<AcaciaRootsSmallCrimson>(), ModContent.TileType<AcaciaRootsSmallHallow>(), ModContent.TileType<AcaciaRootsSmall>()) is int value && value != -1)
		{
			TileExtensions.GetTopLeft(ref i, ref j);
			ConversionHelper.ConvertTiles(i, j, 2, 1, value);
		}
	}
}

public class AcaciaRootsSmallCorrupt : AcaciaRootsSmall
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Corrupt[Type] = true;
		DustType = DustID.Ebonwood;
	}
}

public class AcaciaRootsSmallCrimson : AcaciaRootsSmall
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Crimson[Type] = true;
		DustType = DustID.Shadewood;
	}
}

public class AcaciaRootsSmallHallow : AcaciaRootsSmall
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Hallow[Type] = true;
		DustType = DustID.Pearlwood;
	}
}