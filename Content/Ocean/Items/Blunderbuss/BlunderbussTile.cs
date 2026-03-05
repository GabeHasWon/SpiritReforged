using SpiritReforged.Common.TileCommon;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.Blunderbuss;

[DrawOrder(DrawOrderAttribute.Layer.Solid)] //Draw over sand
public class BlunderbussTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.CoordinateHeights = [18];
		TileObjectData.newTile.Origin = new(1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 100, 50));
		RegisterItemDrop(ModContent.ItemType<Blunderbuss>());

		DustType = DustID.Sand; //No dust
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<Blunderbuss>();
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		var tile = Framing.GetTileSafely(i, j);
		type = (tile.TileFrameY / 18) switch
		{
			1 => DustID.Corruption,
			2 => DustID.Crimson,
			3 => DustID.Pearlsand,
			_ => DustID.Sand,
		};

		return true;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		var drops = base.GetItemDrops(i, j);
		drops = drops.Concat([new Item(ItemID.MusketBall, 30)]); //Guarantee an additional 30 musket balls

		return drops;
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		int type = Framing.GetTileSafely(i, j + 1).TileType;

		switch (type)
		{
			case TileID.Sand:
				Framing.GetTileSafely(i, j).TileFrameY = 0;
				break;

				case TileID.Ebonsand:
				Framing.GetTileSafely(i, j).TileFrameY = 18;
				break;

				case TileID.Crimsand:
				Framing.GetTileSafely(i, j).TileFrameY = 36;
				break;

				case TileID.Pearlsand:
				Framing.GetTileSafely(i, j).TileFrameY = 54;
				break;
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 18);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 2);

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

		return false;
	}
}
