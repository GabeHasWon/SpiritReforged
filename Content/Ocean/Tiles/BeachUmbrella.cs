using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.DrawPreviewHook;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Tiles;

public class BeachUmbrella : ModTile, IDrawPreview, IAutoloadTileItem, IModifySmartTarget
{
	public void SetItemDefaults(ModItem item) => item.Item.value = Item.buyPrice(silver: 20);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileLighted[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 1;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new Point16(0, 2);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidWithTop | AnchorType.SolidTile | AnchorType.Table, 1, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidWithTop | AnchorType.SolidTile | AnchorType.Table, 1, 1);
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(155, 154, 171));
		DustType = -1;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var t = Main.tile[i, j];
		if (t.TileFrameY == 0)
		{
			if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
				return false;

			CustomDraw(i, j, spriteBatch, TileObjectData.GetTileData(t), texture, color);
		}

		return false;
	}

	private static void CustomDraw(int i, int j, SpriteBatch spriteBatch, TileObjectData data, Texture2D texture, Color color)
	{
		bool flipped = data.Style == 1;
		var sizeOffset = new Point(flipped ? 1 : 2, 2);

		for (int frameX = 0; frameX < 4; frameX++)
		{
			for (int frameY = 0; frameY < 5; frameY++)
			{
				(int x, int y) = (i + frameX - sizeOffset.X, j + frameY - sizeOffset.Y);

				var source = new Rectangle((flipped ? 4 * 18 : 0) + frameX * 18, frameY * 18, 16, (frameY == 4) ? 18 : 16);
				var drawPos = new Vector2(x, y) * 16 - Main.screenPosition + TileExtensions.TileOffset;

				spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
			}
		}
	}

	public void DrawPreview(SpriteBatch spriteBatch, TileObjectPreviewData op, Vector2 position)
	{
		var data = TileObjectData.GetTileData(op.Type, op.Style, op.Alternate);
		var color = ((op[0, 0] == 1) ? Color.White : Color.Red * .7f) * .5f;

		CustomDraw(op.Coordinates.X, op.Coordinates.Y, spriteBatch, data, TextureAssets.Tile[Type].Value, color);
	}

	public void ModifyTarget(ref int x, ref int y)
	{
		while (Main.tile[x, y - 1].TileType == Type)
			y--;
	}
}
