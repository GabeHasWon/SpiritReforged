using Terraria.Graphics;

namespace SpiritReforged.Common.WallCommon;

public static class WallMethods
{
	public static void DrawSingleWall(int i, int j)
	{
		var tile = Main.tile[i, j];
		DrawSingleWall(i, j, tile.WallType, new(tile.WallFrameX, tile.WallFrameY + Main.wallFrame[tile.WallType] * 180, 32, 32), 1);
	}

	public static void DrawSingleWall(int i, int j, ushort type, Rectangle source, float opacity)
	{
		if (type == WallID.None)
			return;

		Tile tile = Main.tile[i, j];

		Main.instance.LoadWall(type);
		Texture2D texture = TextureAssets.Wall[type].Value;
		Vector2 position = new Vector2(i, j).ToWorldCoordinates(-8, -8) - Main.screenPosition;

		if (Lighting.NotRetro && !Main.wallLight[type])
		{
			if (tile.WallColor != PaintID.None)
			{
				var painted = Main.instance.TilePaintSystem.TryGetWallAndRequestIfNotReady(type, tile.WallColor);
				texture = painted ?? texture;
			}

			Lighting.GetCornerColors(i, j, out var vertices);
			if (tile.IsWallFullbright)
				vertices = new VertexColors(Color.White);

			vertices.TopLeftColor *= opacity;
			vertices.TopRightColor *= opacity;
			vertices.BottomRightColor *= opacity;
			vertices.BottomLeftColor *= opacity;

			Main.tileBatch.Draw(texture, position, source, vertices, Vector2.Zero, 1, SpriteEffects.None);
		}
		else
		{
			Color color = Lighting.GetColor(i, j);
			if (tile.IsWallFullbright || type == 318)
				color = Color.White;

			Main.spriteBatch.Draw(texture, position, source, color * opacity, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		}
	}
}