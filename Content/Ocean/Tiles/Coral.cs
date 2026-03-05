using RubbleAutoloader;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.DrawPreviewHook;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Tiles;

public class Coral3x3 : ModTile, IDrawPreview, IAutoloadRubble
{
	public virtual Point FrameOffset => Point.Zero;

	public override string Texture => base.Texture.Remove(base.Texture.Length - 3, 3); //Remove the size signature

	public virtual IAutoloadRubble.RubbleData Data => new(ItemID.Coral, IAutoloadRubble.RubbleSize.Large);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		SetObjectData();

		TileID.Sets.DisableSmartCursor[Type] = true;
		DustType = DustID.Coralstone;
		AddMapEntry(new Color(87, 61, 51), Language.GetText("Mods.SpiritReforged.Tiles.CoralMapEntry"));
	}

	public virtual void SetObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Ebonsand, TileID.Pearlsand];
		TileObjectData.newTile.RandomStyleRange = 1;
		TileObjectData.addTile(Type);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Coral) { stack = Main.rand.Next(6, 12) };
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Framing.GetTileSafely(i, j);
		var texture = TextureAssets.Tile[tile.TileType].Value;

		var frame = new Point(tile.TileFrameX + 18 * FrameOffset.X, tile.TileFrameY + 18 * FrameOffset.Y);
		var source = new Rectangle(frame.X, frame.Y, 16, 16);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset;

		spriteBatch.Draw(texture, position, source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		return false;
	}

	public void DrawPreview(SpriteBatch spriteBatch, TileObjectPreviewData op, Vector2 pos)
	{
		var texture = TextureAssets.Tile[op.Type].Value;

		for (int frameX = 0; frameX < op.Size.X; frameX++)
		{
			for (int frameY = 0; frameY < op.Size.Y; frameY++)
			{
				(int x, int y) = (op.Coordinates.X + frameX, op.Coordinates.Y + frameY);

				var color = ((op[frameX, frameY] == 1) ? Color.White : Color.Red * .7f) * .5f;
				var frame = new Point(frameX * 18 + 36 * op.Style + 18 * FrameOffset.X, frameY * 18 + 18 * FrameOffset.Y);
				var source = new Rectangle(frame.X, frame.Y, 16, 16);
				var position = new Vector2(x, y) * 16 - Main.screenPosition + TileExtensions.TileOffset;

				spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
			}
		}
	}
}

public class Coral2x2 : Coral3x3
{
	public override Point FrameOffset => new(3, 1);

	public override IAutoloadRubble.RubbleData Data => new(ItemID.Coral, IAutoloadRubble.RubbleSize.Medium);

	public override void SetObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Ebonsand, TileID.Pearlsand];
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.addTile(Type);
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Coral) { stack = Main.rand.Next(3, 6) };
	}
}

public class Coral1x2 : Coral3x3
{
	public override Point FrameOffset => new(9, 1);

	public override IAutoloadRubble.RubbleData Data => new(ItemID.Coral, IAutoloadRubble.RubbleSize.Small);

	public override void SetObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.Width = 1;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Ebonsand, TileID.Pearlsand];
		TileObjectData.newTile.RandomStyleRange = 1;
		TileObjectData.addTile(Type);
	}

	public override void SetSpriteEffects(int i, int j, ref SpriteEffects spriteEffects) => spriteEffects = (i % 2 == 0) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Coral);
	}
}