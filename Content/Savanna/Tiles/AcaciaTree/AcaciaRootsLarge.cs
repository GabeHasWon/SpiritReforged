using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

public class AcaciaRootsLarge : ModTile, ISetConversion
{
	public virtual ConversionHandler.Set ConversionSet => ConversionHelper.CreateSimple(ModContent.TileType<AcaciaRootsLargeCorrupt>(),
		ModContent.TileType<AcaciaRootsLargeCrimson>(), ModContent.TileType<AcaciaRootsLargeHallow>(), ModContent.TileType<AcaciaRootsLarge>());

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassMowed>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaDirt>()];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.RandomStyleRange = 4;
		TileObjectData.addTile(Type);

		DustType = DustID.WoodFurniture;
		RegisterItemDrop(AutoContent.ItemType<Drywood>());
		AddMapEntry(new Color(87, 61, 51));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHandler.FindSet(nameof(AcaciaRootsLarge), conversionType, out int newType) && Type != newType)
		{
			TileExtensions.GetTopLeft(ref i, ref j);
			ConversionHelper.ConvertTiles(i, j, 3, 1, newType);
		}
	}
}

public class AcaciaRootsLargeCorrupt : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Corrupt[Type] = true;
		DustType = DustID.Ebonwood;
	}
}

public class AcaciaRootsLargeCrimson : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Crimson[Type] = true;
		DustType = DustID.Shadewood;
	}
}

public class AcaciaRootsLargeHallow : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileID.Sets.Hallow[Type] = true;
		DustType = DustID.Pearlwood;
	}
}