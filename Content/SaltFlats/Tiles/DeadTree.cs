using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class DeadTree : CustomTree
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SaltBlockDull>()];

		AddMapEntry(new Color(120, 80, 75), Language.GetText("MapObject.Tree"));
		RegisterItemDrop(AutoContent.ItemType<Drywood>());
		DustType = DustID.Pearlwood;
	}

	public override bool IsTreeTop(int i, int j)
	{
		if (!WorldGen.InWorld(i, j) || ModContent.GetModTile(Main.tile[i, j].TileType) is not DeadTree || ModContent.GetModTile(Main.tile[i, j - 1].TileType) is DeadTree)
			return false;

		return true;
	}

	protected override float Noise(Vector2 position) => base.Noise(position);

	public override void DrawTreeFoliage(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return;

		var position = new Vector2(i, j) * 16 - Main.screenPosition + TreeExtensions.GetPalmTreeOffset(i, j);
		float rotation = 0;
		int tileFrameX = i;

		if (IsTreeTop(i, j)) //Draw treetops
		{
			Point size = new(210, 156);
			int frameY = tileFrameX % 3;
			var source = new Rectangle(196, 20 + (size.Y + 2) * frameY, size.X, size.Y);
			var origin = new Vector2(source.Width / 2, source.Height);

			spriteBatch.Draw(texture, position + new Vector2(9, 3), source, color, rotation, origin, 1, SpriteEffects.None, 0);
		}
		else //Draw branches
		{
			int frameX = (Noise(new Vector2(i, j)) > 0) ? 1 : 0;
			int frameY = tileFrameX % 6;

			Point size = new(96, 98);
			var source = new Rectangle((size.X + 2) * frameX, 20 + (size.Y + 2) * frameY, size.X, size.Y);
			var origin = new Vector2(frameX == 0 ? source.Width : 0, 80);

			position += new Vector2(6 * ((frameX == 0) ? -1 : 1), 8); //Directional offset

			spriteBatch.Draw(texture, position + new Vector2(10, 0), source, color, rotation, origin, 1, SpriteEffects.None, 0);
		}
	}

	protected override void CreateTree(int i, int j, int height)
	{
		int variance = WorldGen.genRand.Next(-8, 9) * 2;
		short xOff = 0;

		height /= 2;

		for (int h = 0; h < height; h++)
		{
			int style = WorldGen.genRand.NextFromList(0, 1);

			WorldGen.PlaceTile(i, j - h, Type, true);
			var tile = Framing.GetTileSafely(i, j - h);

			if (tile.HasTile && tile.TileType == Type)
			{
				Framing.GetTileSafely(i, j - h).TileFrameX = (short)(style * FrameSize);
				Framing.GetTileSafely(i, j - h).TileFrameY = TreeExtensions.GetPalmOffset(j, variance, height, ref xOff);
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j + 1 - height, 1, height, TileChangeType.None);
	}
}