using SpiritReforged.Common;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Savanna.Items;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("230,230,195", false)]
public class Starflower : ModTile, ISwayTile
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

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (.3f, .28f, .1f);

	public override void Convert(int i, int j, int conversionType)
	{
		if (conversionType is BiomeConversionID.Purity or BiomeConversionID.PurificationPowder)
		{
			int type = Main.tile[i, j].TileType;

			if (Framing.GetTileSafely(i, j + 1).TileType == type)
				return; //Return if this is not the base of the flower

			TileExtensions.GetTopLeft(ref i, ref j);
			ConversionHelper.ConvertTiles(i, j, 2, 4, TileID.Sunflower);
		}
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var t = Main.tile[i, j];
		var data = TileObjectData.GetTileData(t);

		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
		int heights = (t.TileFrameY == 54) ? 18 : 16;

		var source = new Rectangle(t.TileFrameX, t.TileFrameY, data.CoordinateWidth, heights);

		spriteBatch.Draw(texture, drawPos + offset, source, color, rotation, origin, 1, SpriteEffects.None, 0);
		spriteBatch.Draw(GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value, drawPos + offset, source, TileExtensions.GetTint(i, j, Color.White), rotation, origin, 1, SpriteEffects.None, 0);
	}

	public float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.Instance.TreeWindCounter);

		if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, data.Width, data.Height))
			rotation = 0f;

		return rotation * .75f;
	}
}
