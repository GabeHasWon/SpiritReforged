using SpiritReforged.Common;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Savanna.Items;
using SpiritReforged.Content.Savanna.Tiles;
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

		SpiritSets.ConvertsByAdjacent[Type] = true;
		TileID.Sets.IsVine[Type] = true;
		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>()];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(24, 135, 28));

		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
	public override void Convert(int i, int j, int conversionType) => ConversionHelper.Simple(i, j, conversionType,
		(BiomeConversionID.Corruption, TileID.CorruptVines),
		(BiomeConversionID.Crimson, TileID.CrimsonVines),
		(BiomeConversionID.Hallow, TileID.HallowedVines),
		(BiomeConversionID.PurificationPowder, TileID.Vines));
}