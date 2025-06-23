using SpiritReforged.Common.TileCommon.Conversion;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;
using static SpiritReforged.Common.TileCommon.Conversion.ConvertAdjacent;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaFoliage : ModTile, IFrameAction
{
	public const int StyleRange = 15;

	public virtual FrameDelegate FrameAction => CommonPlants;

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

		DustType = -1;
		HitSound = SoundID.Grass;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinateHeights = [TileHeight];
		TileObjectData.newTile.DrawYOffset = -(TileHeight - 18);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = StyleRange;

		PreAddObjectData();

		TileObjectData.addTile(Type);
	}

	public virtual void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>()];

		DustType = DustID.JunglePlants;
		AddMapEntry(new(104, 156, 7));
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
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCorrupt>()];

		DustType = DustID.Corruption;
		AddMapEntry(new(109, 106, 174));
	}
}

public class SavannaFoliageCrimson : SavannaFoliage
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCrimson>()];

		DustType = DustID.CrimsonPlants;
		AddMapEntry(new(183, 69, 68));
	}
}

public class SavannaFoliageHallow : SavannaFoliage
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassHallow>()];

		DustType = DustID.HallowedPlants;
		AddMapEntry(new(78, 193, 227));
	}
}
