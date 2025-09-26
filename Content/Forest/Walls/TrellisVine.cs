using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Walls;

public class TrellisVine : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
		TileObjectData.newTile.Width = TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.Origin = Point16.Zero;
		TileObjectData.newTile.CoordinateWidth = 24;
		TileObjectData.newTile.CoordinateHeights = [22];
		TileObjectData.newTile.RandomStyleRange = 5;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(90, 165, 100));
		HitSound = SoundID.Grass with { Pitch = 0.2f, PitchVariance = 0.1f };
		DustType = DustID.Grass;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
		{
			Tile tile = Main.tile[i, j];
			var data = TileObjectData.GetTileData(tile);
			var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, data.CoordinateWidth, data.CoordinateFullHeight);
			var position = new Vector2(i, j).ToWorldCoordinates(8, 0) - Main.screenPosition + TileExtensions.TileOffset;
			var origin = new Vector2(source.Width / 2, 4);

			if (Framing.GetTileSafely(i + 1, j + 1).TileType == Type)
			{
				const float lightness = 0.6f;
				spriteBatch.Draw(texture, position + new Vector2(8), source, color.MultiplyRGB(new Color(lightness, lightness, lightness)), 0, origin, 1, SpriteEffects.None, 0);
			}

			spriteBatch.Draw(texture, position, source, color, 0, origin, 1, SpriteEffects.None, 0);
		}

		return false;
	}
}