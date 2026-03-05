using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using static Terraria.GameContent.Drawing.TileDrawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class VineTile : ModTile, ISwayTile
{
	public int Style => (int)TileCounterType.Vine;

	public override void SetStaticDefaults()
	{
		Main.tileBlockLight[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.IsVine[Type] = true;
		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

		HitSound = SoundID.Grass;
		DustType = DustID.Grass;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorAlternateTiles = [Type];

		PreAddObjectData();

		if (TileObjectData.newTile.AnchorValidTiles is int[] array && array.Length > 0)
			ConversionHandler.CreateSet(ConversionHandler.Vines, new() { { array[0], Type } });

		TileObjectData.addTile(Type);
	}

	public virtual void PreAddObjectData() { }

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		ConversionHandler.CommonVines(i, j, Type);
		return true;
	}

	public static void ConvertVines(int i, int j, int newType)
	{
		int type = Main.tile[i, j].TileType;

		if (Framing.GetTileSafely(i, j - 1).TileType == type)
			return; //Return if this is not the base of the vine

		int bottom = j;
		while (WorldGen.InWorld(i, bottom, 2) && Main.tile[i, bottom].TileType == type)
			bottom++; //Iterate to the bottom of the vine

		int height = bottom - j;
		ConversionHelper.ConvertTiles(i, j, 1, height, newType);
	}
}