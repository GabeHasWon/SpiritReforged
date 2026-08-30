using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Ocean.Tiles;

public class BeachUmbrella : ModTile, WindTileRenderer.IDrawInWind, ILoadItem, IModifySmartTarget
{
	public void SetItemDefaults(ModItem item) => item.Item.value = Item.buyPrice(silver: 20);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		Main.tileFrameImportant[Type] = true;

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

	void WindTileRenderer.IDrawInWind.DrawInWind(SpriteBatch spriteBatch, int i, int j, float rotation, Vector2 position, Vector2 origin)
	{
		const int width = 4;
		const int height = 5;

		Tile tile = Main.tile[i, j];
		if (TileDrawing.IsVisible(tile) && TileObjectData.IsTopLeft(i, j) && TileObjectData.GetTileData(Main.tile[i, j]) is TileObjectData tileObjectData)
		{
			Texture2D texture = Helpers.GetTileTextureValue(tile);
			float physics = (this as WindTileRenderer.IDrawInWind).GetWindStrength(i, j);
			bool flipped = tile.TileFrameX == 18;

			for (int x = i; x < i + width; x++)
			{
				for (int y = j; y < j + height; y++)
				{
					(int gridX, int gridY) = (x - i, y - j);
					Rectangle source = new(gridX * 18 + (flipped ? (18 * width) : 0), gridY * 18, 16, 18);

					rotation = (1.5f - gridY / (height - 1f)) * physics * 0.1f;
					position = new Vector2(x, y) * 16 - Main.screenPosition + new Vector2(0, Math.Abs(rotation) * 20f);
					origin = (tileObjectData.Origin.ToVector2() + Vector2.One * 2.5f - new Vector2(gridX, gridY)) * 16;

					spriteBatch.Draw(texture, position + origin - new Vector2(16 * 2), source, Lighting.GetColor(x, y), rotation, origin, 1, SpriteEffects.None, 0);
				}
			}
		}
	}

	float WindTileRenderer.IDrawInWind.GetWindStrength(int i, int j)
	{
		if (TileObjectData.GetTileData(Framing.GetTileSafely(i, j)) is TileObjectData tileObjectData)
		{
			float rotation = WorldGen.InAPlaceWithWind(i, j, tileObjectData.Width, tileObjectData.Height) ? Main.instance.TilesRenderer.GetWindCycle(i, j, WindTileRenderer.TreeWindCounter) : 0f;
			return (rotation + WindTileRenderer.GetHighestWindGridPushComplex(i, j, tileObjectData.Width, tileObjectData.Height, 40, 1f, 3, true)) * 0.5f;
		}

		return 0f;
	}

	public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
	{
		const int width = 4;
		const int height = 5;

		if (frame.Y == 0)
		{
			Texture2D texture = TextureAssets.Tile[Type].Value;
			bool flipped = frame.X == 18;

			for (int x = i; x < i + width; x++)
			{
				for (int y = j; y < j + height; y++)
				{
					(int gridX, int gridY) = (x - i, y - j);

					position = new Vector2(x, y) * 16f - Main.screenPosition + (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
					Rectangle source = new(gridX * 18 + (flipped ? (18 * width) : 0), gridY * 18, 16, 18);

					spriteBatch.Draw(texture, position - new Vector2(16 * 2), source, color, 0, Vector2.Zero, 1, spriteEffects, 0);
				}
			}
		}

		return false;
	}

	public void ModifyTarget(ref int x, ref int y)
	{
		while (Main.tile[x, y - 1].TileType == Type)
			y--;
	}
}