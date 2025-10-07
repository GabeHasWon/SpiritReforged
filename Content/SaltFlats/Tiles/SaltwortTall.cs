using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltwortTall : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.WaterDeath = false;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateWidth = 28;
		TileObjectData.newTile.CoordinateHeights = [32];
		TileObjectData.newTile.DrawYOffset = -14;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SaltBlockDull>()];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(190, 80, 100));
		DustType = DustID.RedStarfish;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;
}