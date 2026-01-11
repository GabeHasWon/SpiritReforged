using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class GooeyVine : ModTile, ISwayTile
{
	public int Style => (int)TileDrawing.TileCounterType.Vine;

	public override void SetStaticDefaults()
	{
		Main.tileBlockLight[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<PaleHive>(), ModContent.TileType<GooeyHive>()];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(140, 140, 100));
		DustType = DustID.Silk;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		Tile above = Framing.GetTileSafely(i, j - 1);
		Tile below = Framing.GetTileSafely(i, j + 1);

		if (!above.HasTile || above.TileType != ModContent.TileType<PaleHive>() && above.TileType != ModContent.TileType<GooeyHive>() && above.TileType != Type)
		{
			WorldGen.KillTile(i, j);
			return false;
		}

		if (resetFrame)
		{
			short frame = (short)(below.HasTileType(Type) ? WorldGen.genRand.NextFromList(0, 18) : 36);
			Main.tile[i, j].TileFrameY = frame;
		}

		return false; //True results in the tile being invisible in most cases
	}
}