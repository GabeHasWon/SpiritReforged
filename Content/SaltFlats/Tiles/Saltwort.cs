using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.GameContent.Metadata;
using TileHelper.Common;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class Saltwort : ModTile, WindTileRenderer.IDrawInWind
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

	void WindTileRenderer.IDrawInWind.DrawInWind(SpriteBatch spriteBatch, int i, int j, float rotation, Vector2 position, Vector2 origin)
	{
		Tile tile = Main.tile[i, j];
		Texture2D texture = Helpers.GetTileTextureValue(tile);
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 18);

		Tile leftTile = Framing.GetTileSafely(i - 1, j);
		if (leftTile.HasTile && leftTile.TileType == Type) //Scan the left tile for drawing an additional layer
		{
			spriteBatch.Draw(texture, position - new Vector2(8, 0), source, Lighting.GetColor(i, j).MultiplyRGB(new(0.8f, 0.7f, 0.5f)) * 0.7f, rotation, origin, 1, SpriteEffects.None, 0);
		}

		spriteBatch.Draw(texture, position, source, Lighting.GetColor(i, j), rotation, origin, 1, SpriteEffects.None, 0);
	}

	float WindTileRenderer.IDrawInWind.GetWindStrength(int i, int j)
	{
		if (TileObjectData.GetTileData(Main.tile[i, j]) is TileObjectData tileObjectData)
		{
			float rotation = WorldGen.InAPlaceWithWind(i, j, tileObjectData.Width, tileObjectData.Height) ? Main.instance.TilesRenderer.GetWindCycle(i, j, WindTileRenderer.GrassWindCounter) * 0.5f : 0f;
			return rotation + Main.instance.TilesRenderer.GetWindGridPush(i, j, 20, 0.15f);
		}

		return 0f;
	}
}