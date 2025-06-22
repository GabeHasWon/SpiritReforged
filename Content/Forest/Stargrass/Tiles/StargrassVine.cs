using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using static Terraria.GameContent.Drawing.TileDrawing;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassTile Glow")] //Use Stargrass' glow
public class StargrassVine : ModTile, ISwayTile
{
	public int Style => (int)TileCounterType.Vine;

	public override void SetStaticDefaults()
	{
		Main.tileBlockLight[Type] = false;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.IsVine[Type] = true;
		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>(), TileID.CorruptGrass, TileID.CrimsonGrass, TileID.HallowedGrass, TileID.Grass];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(24, 135, 28));

		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void Convert(int i, int j, int conversionType)
	{
		int type = Main.tile[i, j].TileType;
		if (Framing.GetTileSafely(i, j - 1).TileType == type)
			return; //Return if this is not the base of the vine

		if (ConversionHelper.FindType(conversionType, type, TileID.CorruptVines, TileID.CrimsonVines, TileID.HallowedVines, TileID.Vines) is int value && value != -1)
		{
			int bottom = j;
			while (WorldGen.InWorld(i, bottom, 2) && Main.tile[i, bottom].TileType == type)
				bottom++; //Iterate to the bottom of the vine

			int height = bottom - j;
			ConversionHelper.ConvertTiles(i, j, 1, height, value);
		}
	}
}