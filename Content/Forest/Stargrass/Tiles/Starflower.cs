using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

public class Starflower : ModTile
{
	public override void Load() => On_Player.FigureOutWhatToPlace += OverrideSunflower;

	/// <summary> Converts Sunflowers into Starflowers on stargrass. </summary>
	private static void OverrideSunflower(On_Player.orig_FigureOutWhatToPlace orig, Player self, Tile targetTile, Item sItem, out int tileToCreate, out int previewPlaceStyle, out bool? overrideCanPlace, out int? forcedRandom)
	{
		orig(self, targetTile, sItem, out tileToCreate, out previewPlaceStyle, out overrideCanPlace, out forcedRandom);

		if (tileToCreate != TileID.Sunflower)
			return;

		var below = Main.tile[Player.tileTargetX, Player.tileTargetY + 1];
		if (WorldGen.SolidTile(below) && below.TileType == ModContent.TileType<StargrassTile>())
			tileToCreate = ModContent.TileType<Starflower>();
	}

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);
		WindTileRenderer.TileDrawInWind[Type] = TileDrawing.TileCounterType.MultiTileGrass;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(0, 3);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>(), TileID.Grass];
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.addTile(Type);

		DustType = DustID.Grass;

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(20, 190, 130), name);
		AddMapEntry(new Color(255, 210, 90), name);
		RegisterItemDrop(ItemID.Sunflower);
	}

	public override ushort GetMapOption(int i, int j)
	{
		var t = Main.tile[i, j];
		return (ushort)((t.TileFrameY < 36) ? 1 : 0);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer)
			Main.SceneMetrics.HasSunflower = true;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.3f, 0.28f, 0.1f);

	public override void Convert(int i, int j, int conversionType)
	{
		if (conversionType is BiomeConversionID.Purity or BiomeConversionID.PurificationPowder)
		{
			int type = Main.tile[i, j].TileType;

			if (Framing.GetTileSafely(i, j + 1).TileType == type)
				return; //Return if this is not the base of the flower

			(i, j) = Helpers.GetTopLeft(i, j);
			ConversionHelper.ConvertTiles(i, j, 2, 4, TileID.Sunflower);
		}
	}
}