using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class Saltwort : ModTile
{
	public const int StyleRange = 7;

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
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinateHeights = [18];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = StyleRange;
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SaltBlockDull>()];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(190, 80, 100));
		DustType = DustID.RedStarfish;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;
}