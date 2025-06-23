using SpiritReforged.Common.TileCommon.Conversion;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaFoliage : ModTile
{
	public const int StyleRange = 15;

	protected virtual Color MapColor => new(104, 156, 7);
	protected virtual int Dust => DustID.JunglePlants;

	public override void SetStaticDefaults()
	{
		const int TileHeight = 30;

		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinateHeights = [TileHeight];
		TileObjectData.newTile.DrawYOffset = -(TileHeight - 18);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = StyleRange;
		TileObjectData.addTile(Type);

		AddMapEntry(MapColor);
		DustType = Dust;
		HitSound = SoundID.Grass;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		ConvertAdjacent.CommonPlant(i, j);
		return true;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HeldItem.type == ItemID.Sickle)
			yield return new Item(ItemID.Hay, Main.rand.Next(1, 3));

		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HasItem(ItemID.Blowpipe))
			yield return new Item(ItemID.Seed, Main.rand.Next(1, 3));
	}
}

public class SavannaFoliageCorrupt : SavannaFoliage
{
	protected override Color MapColor => new(109, 106, 174);
	protected override int Dust => DustID.Corruption;
}

public class SavannaFoliageCrimson : SavannaFoliage
{
	protected override Color MapColor => new(183, 69, 68);
	protected override int Dust => DustID.CrimsonPlants;
}

public class SavannaFoliageHallow : SavannaFoliage
{
	protected override Color MapColor => new(78, 193, 227);
	protected override int Dust => DustID.HallowedPlants;
}
