using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class DeadTree : CustomTree
{
	public override int TreeHeight => WorldGen.genRand.Next(3, 11);

	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SaltBlockDull>()];

		AddMapEntry(new Color(120, 80, 75), Language.GetText("MapObject.Tree"));
		RegisterItemDrop(AutoContent.ItemType<Drywood>());
		DustType = DustID.Pearlwood;
	}

	public override SegmentType FindSegment(int i, int j)
	{
		if (Main.tile[i, j].TileFrameX > FrameSize * 6)
			return SegmentType.Bare;

		int type = Framing.GetTileSafely(i, j - 1).TileType;
		return (type != Type) ? SegmentType.LeafyTop : SegmentType.Default;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly)
		{
			if (!fail) //Switch to the 'chopped' frame
				Framing.GetTileSafely(i, j + 1).TileFrameX = (short)(WorldGen.genRand.Next(10, 12) * FrameSize);
			else
				ShakeTree(i, j);
		}
	}

	protected override void OnShakeTree(int i, int j)
	{
		if (Main.rand.NextBool(3))
		{
			Vector2 position = new Vector2(i, j - 3) * 16;
			Item.NewItem(null, new Rectangle((int)position.X, (int)position.Y, 16, 16), AutoContent.ItemType<Drywood>(), Main.rand.Next(3, 11));
		}

		GrowEffects(i, j, true);
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		Tile tile = Main.tile[i, j];
		Tile right = Framing.GetTileSafely(i + 1, j);
		Tile left = Framing.GetTileSafely(i - 1, j);
		Tile top = Framing.GetTileSafely(i, j - 1);

		if (top.HasTileType(Type))
		{
			if (left.HasTileType(Type) && right.HasTileType(Type))
				tile.TileFrameX = 14 * FrameSize;
			else if (right.HasTileType(Type))
				tile.TileFrameX = 12 * FrameSize;
			else if (left.HasTileType(Type))
				tile.TileFrameX = 13 * FrameSize;
		}
		else if (!left.HasTileType(Type) && !right.HasTileType(Type) && tile.TileFrameX >= FrameSize * 15)
		{
			WorldGen.KillTile(i, j);
		}

		return base.TileFrame(i, j, ref resetFrame, ref noBreak);
	}

	public override void DrawTreeFoliage(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return;

		var position = new Vector2(i, j) * 16 - Main.screenPosition + TreeExtensions.GetPalmTreeOffset(i, j);
		float rotation = Main.instance.TilesRenderer.GetWindCycle(i, j, TileSwaySystem.Instance.TreeWindCounter) * 0.05f;
		int tileFrameX = i;

		if (FindSegment(i, j) is SegmentType.LeafyTop) //Draw treetops
		{
			Point size = new(210, 156);
			int frameY = tileFrameX % 5;
			var source = new Rectangle(196, 20 + (size.Y + 2) * frameY, size.X, size.Y);
			var origin = new Vector2(source.Width / 2, source.Height);

			spriteBatch.Draw(texture, position + new Vector2(7, 2), source, color, rotation, origin, 1, SpriteEffects.None, 0);
		}
		else //Draw branches
		{
			int frameX = Random(i, j, 0, 2);
			int frameY = tileFrameX % 3;

			Point size = new(96, 98);
			var source = new Rectangle((size.X + 2) * frameX, 20 + (size.Y + 2) * frameY, size.X, size.Y);
			var origin = new Vector2((frameX == 0) ? source.Width : 0, 80);

			position += new Vector2(4 * ((frameX == 0) ? -1 : 1), 8); //Directional offset

			spriteBatch.Draw(texture, position + new Vector2(10, 0), source, color, rotation, origin, 1, SpriteEffects.None, 0);
		}
	}

	protected override void CreateTree(int i, int j, int height)
	{
		int variance = WorldGen.genRand.NextFromList(-4, 4, -6, 6, -8, 8) * 2;
		short xOff = 0;

		for (int h = 0; h < height; h++)
		{
			int style = WorldGen.genRand.NextFromList(0, 1);
			short offset = TreeExtensions.GetPalmOffset(j, variance, height, ref xOff);

			PlaceSegment(i, j - h, (short)(style * FrameSize), offset);

			if (h == 0)
			{
				if (WorldGen.genRand.NextBool())
					PlaceSegment(i - 1, j, (short)(WorldGen.genRand.NextFromList(15, 17, 19) * FrameSize), offset);

				if (WorldGen.genRand.NextBool())
					PlaceSegment(i + 1, j, (short)(WorldGen.genRand.NextFromList(16, 18, 20) * FrameSize), offset);
			}
		}

		WorldGen.SquareTileFrame(i, j);

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i - 1, j + 1 - height, 3, height, TileChangeType.None);

		static void PlaceSegment(int i, int j, short frameX, short frameY)
		{
			int type = ModContent.TileType<DeadTree>();
			if (Placer.PlaceTile(i, j, type).success)
			{
				Main.tile[i, j].TileFrameX = frameX;
				Main.tile[i, j].TileFrameY = frameY;
			}
		}
	}
}