using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.DrawPreviewHook;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class BonsaiTrees : ModTile, IDrawPreview
{
	public const int FrameWidth = 60;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.CoordinateWidth = 18;
		TileObjectData.newTile.Origin = new(1, 3);
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.newTile.StyleWrapLimit = 2; 
		TileObjectData.newTile.StyleMultiplier = 2; 
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; 
		TileObjectData.addAlternate(1); 
		TileObjectData.addTile(Type);

		DustType = -1;
		AddMapEntry(new Color(140, 140, 140), CreateMapEntryName());
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var frame = new Point(t.TileFrameX, t.TileFrameY);

		int offsetX = (frame.X % FrameWidth == 0) ? -2 : ((frame.X % FrameWidth == 40) ? 2 : 0);

		var source = new Rectangle(frame.X, frame.Y, 18, 16);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(offsetX, 0);

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		return false;
	}

	public void DrawPreview(SpriteBatch spriteBatch, TileObjectPreviewData op, Vector2 position)
	{
		const int wrap = 2;

		var texture = TextureAssets.Tile[op.Type].Value;
		var data = TileObjectData.GetTileData(op.Type, op.Style, op.Alternate);
		var color = ((op[0, 0] == 1) ? Color.White : Color.Red * .7f) * .5f;

		int style = data.CalculatePlacementStyle(op.Style, op.Alternate, op.Random);

		for (int frameX = 0; frameX < 3; frameX++)
		{
			for (int frameY = 0; frameY < 4; frameY++)
			{
				(int x, int y) = (op.Coordinates.X + frameX, op.Coordinates.Y + frameY);

				var source = new Rectangle(frameX * 20 + style % wrap * data.CoordinateFullWidth, frameY * 18 + style / wrap * data.CoordinateFullHeight, 18, 16);
				int offsetX = (frameX == 0) ? -2 : ((frameX == 2) ? 2 : 0);
				var drawPos = new Vector2(x, y) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(offsetX, 0);

				spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
			}
		}
	}
}