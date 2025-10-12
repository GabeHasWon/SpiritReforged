using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class Saltwort : ModTile, ISwayTile
{
	public const int StyleRange = 7;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;

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
	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var t = Main.tile[i, j];
		var texture = TextureAssets.Tile[Type].Value;
		var position = new Vector2(i, j) * 16 - Main.screenPosition;
		var source = new Rectangle(t.TileFrameX, 0, 16, 20);

		var leftTile = Framing.GetTileSafely(i - 1, j);
		if (leftTile.HasTile && leftTile.TileType == Type) //Scan the left tile for drawing an additional layer
		{
			spriteBatch.Draw(texture, position + offset - new Vector2(8, 0), source, Lighting.GetColor(i, j).MultiplyRGB(new(0.8f, 0.7f, 0.5f)) * 0.7f, rotation, origin, 1, default, 0);
		}

		spriteBatch.Draw(texture, position + offset, source, Lighting.GetColor(i, j), rotation, origin, 1, default, 0);
	}

	public float Physics(Point16 coords)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(coords));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(coords.X, coords.Y, TileSwaySystem.GrassWindCounter);

		if (!WorldGen.InAPlaceWithWind(coords.X, coords.Y, data.Width, data.Height))
			rotation = 0f;

		return rotation + Main.instance.TilesRenderer.GetWindGridPush(coords.X, coords.Y, 20, 0.35f) * 1.5f;
	}
}