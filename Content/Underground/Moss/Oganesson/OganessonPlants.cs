using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Dusts;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

public class OganessonPlants : ModTile, ICheckItemUse
{
	public enum Side : byte
	{
		Up,
		Left,
		Right,
		Down
	}

	public const int StyleRange = 3;

	public override void SetStaticDefaults()
	{
		TileDefaults();
		ObjectData(new Color(180, 180, 180), ModContent.TileType<OganessonMoss>(), ModContent.TileType<OganessonMossGrayBrick>());

		DustType = ModContent.DustType<OganessonMossDust>();
		HitSound = SoundID.Grass;
	}

	public void TileDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;

		TileID.Sets.SwaysInWindBasic[Type] = true;
	}

	public virtual void ObjectData(Color mapEntry, params int[] anchors)
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = anchors;
		TileObjectData.newTile.RandomStyleRange = StyleRange;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(StyleRange);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(StyleRange * 2);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(StyleRange * 3);

		TileObjectData.addTile(Type);

		AddMapEntry(mapEntry);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = Main.rand.Next(1, 3);
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.3f, 0.3f, 0.3f);

	public virtual bool? CheckItemUse(int type, Player player, int i, int j)
	{
		if (type is ItemID.PaintScraper or ItemID.SpectrePaintScraper || type == ModContent.ItemType<LandscapingShears>())
		{
			WorldGen.KillTile(i, j);
			return true;
		}

		return null;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		int heldType = Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(), 16, 16)].HeldItem.type;
		int drop = ModContent.ItemType<OganessonMossItem>();

		if (heldType == ModContent.ItemType<LandscapingShears>())
		{
			if (Main.rand.NextBool(2))
				yield return new Item(drop);
		}
		else if (heldType is ItemID.PaintScraper or ItemID.SpectrePaintScraper)
		{
			if (Main.rand.NextBool(9))
				yield return new Item(drop);
		}
	}

	public override void MouseOver(int i, int j)
	{
		var p = Main.LocalPlayer;
		int type = p.HeldItem.type;

		if (type is ItemID.PaintScraper or ItemID.SpectrePaintScraper || type == ModContent.ItemType<LandscapingShears>())
		{
			p.cursorItemIconEnabled = true;
			p.cursorItemIconID = ItemID.None;
			p.noThrow = 2;
		}
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		const int styleLength = 3 * 18;

		var t = Main.tile[i, j];
		int frameX = resetFrame ? Main.rand.Next(3) * 18 : t.TileFrameX % styleLength;

		t.TileFrameX = CheckSide(out bool failed);

		if (resetFrame)
			t.TileFrameY = 0;

		if (failed)
			WorldGen.KillTile(i, j);

		short CheckSide(out bool failed)
		{
			Side side;

			if (ValidSurface(i, j + 1))
				side = Side.Up;
			else if (ValidSurface(i, j - 1))
				side = Side.Down;
			else if (ValidSurface(i + 1, j))
				side = Side.Left;
			else if (ValidSurface(i - 1, j))
				side = Side.Right;
			else
			{
				failed = true;
				return t.TileFrameX;
			}

			failed = false;
			return (short)((int)side * styleLength + frameX);
		}

		bool ValidSurface(int i, int j)
		{
			var connectT = Framing.GetTileSafely(i, j);
			return WorldGen.SolidTile(connectT) && TileObjectData.GetTileData(t) is TileObjectData data && data.AnchorValidTiles.Contains(connectT.TileType);
		}

		return false;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile t = Main.tile[i, j];

		var texture = TextureAssets.Tile[Type].Value;
		var source = new Rectangle(t.TileFrameX, t.TileFrameY, 18, 18);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset;

		position += (t.TileFrameX / 18 / StyleRange) switch
		{
			(byte)Side.Left => new Vector2(2, 0),
			(byte)Side.Right => new Vector2(-2, 0),
			(byte)Side.Down => new Vector2(0, -2),
			_ => new Vector2(0, 2),
		};

		spriteBatch.Draw(texture, position, source, Color.White, 0, Vector2.Zero, 1, default, 0);
		return false;
	}
}