using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.WorldGeneration.Noise;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.CactiVariants;

internal partial class BunnyEarCacti : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type] = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 2, 0);
		TileObjectData.newTile.StyleHorizontal = false;
		TileObjectData.newTile.RandomStyleRange = 1;
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand];

		BunnyEarCactiTE tileEntity = ModContent.GetInstance<BunnyEarCactiTE>();
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(146, 188, 61), this.GetLocalization("MapEntry"));
		DustType = DustID.t_Cactus;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 2 : 6;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		// Prickly Pear drop done in the TE for position reasons
		yield return new Item(ItemID.Cactus, Main.rand.Next(2, 5));
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY) => ModContent.GetInstance<BunnyEarCactiTE>().Kill(i - frameX % 36 / 18, j - frameY % 36 / 18);

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out var te) || te is not BunnyEarCactiTE bunny)
			return false;

		if (!TileExtensions.GetVisualInfo(i, j, out _, out Texture2D tex))
			return false;

		for (int k = 0; k < bunny.Segments.Count; ++k)
		{
			BunnyEarCactiTE.Segment segment = bunny.Segments[k];
			DrawSingleSegment(i, j, spriteBatch, tex, segment);
		}

		return false;
	}

	private static void DrawSingleSegment(int i, int j, SpriteBatch spriteBatch, Texture2D tex, BunnyEarCactiTE.Segment segment)
	{
		Rectangle src = new(0, 34 * segment.Style, 30, 32);
		Vector2 position = new Vector2(i + segment.XOffset * 0.7f, j - segment.Layer * 1.5f) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

		if (segment.Alt)
			position += new Vector2(-MathF.Sign(segment.XOffset) * 4, 16);

		Point16 tilePos = (position + Main.screenPosition - TileExtensions.TileOffset).ToTileCoordinates16();
		Color color = Lighting.GetColor(tilePos.X, tilePos.Y);
		float baseRot = Main.instance.TilesRenderer.GetWindCycle(tilePos.X, tilePos.Y, TileSwaySystem.Instance.TreeWindCounter) * MathF.Min(segment.Layer / 2f, 1);
		position.X += baseRot * 2f;
		float rotation = segment.Rotation + baseRot * 0.05f;
		Vector2 origin = src.Size() / 2f;
		spriteBatch.Draw(tex, position + origin, src, color, rotation, origin, 1, SpriteEffects.None, 0);

		if (segment.MiniTops != -1)
			DrawTops(position, tex, segment, spriteBatch, rotation, origin, new Point16(tilePos.X, tilePos.Y - segment.Layer));
	}

	private static void DrawTops(Vector2 position, Texture2D tex, BunnyEarCactiTE.Segment segment, SpriteBatch batch, float rotation, Vector2 origin, Point16 basePos)
	{
		int style = segment.MiniTops / 2;
		int count = segment.MiniTops / 6 + 1;
		var src = new Rectangle(32, 16 * style, 16, 16);

		Vector2 pos = position + origin + (rotation - MathHelper.PiOver2).ToRotationVector2() * 22;
		batch.Draw(tex, pos, src, Lighting.GetColor(basePos.ToPoint()), rotation, src.Size() / 2f, 1f, SpriteEffects.None, 0);

		if (count == 2)
		{
			float noise = NoiseSystem.PerlinStatic(basePos.X, basePos.Y);
			style += (int)MathF.Abs(noise * 30);
			style %= 6;
			src = new Rectangle(32, 16 * style, 16, 16);

			if (noise >= 0.5f)
			{
				Vector2 secondTopPos = position + origin + (rotation - MathHelper.PiOver4).ToRotationVector2() * 20;
				batch.Draw(tex, secondTopPos, src, Lighting.GetColor(basePos.ToPoint()), rotation + 1, src.Size() / 2f, 1f, SpriteEffects.None, 0);
			}
			else
			{
				Vector2 secondTopPos = position + origin + (rotation - MathHelper.PiOver4 * 3).ToRotationVector2() * 20;
				batch.Draw(tex, secondTopPos, src, Lighting.GetColor(basePos.ToPoint()), rotation - 1, src.Size() / 2f, 1f, SpriteEffects.None, 0);
			}
		}
	}

	public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch batch, ref Rectangle frame, ref Vector2 pos, ref Color color, bool valid, ref SpriteEffects fx)
	{
		Texture2D tex = TextureAssets.Tile[Type].Value;

		if (frame.X != 0 || frame.Y != 0)
			return false;

		frame = new Rectangle(0, 0, 30, 32);
		batch.Draw(tex, pos, frame, color, 0f, Vector2.Zero, 1f, fx, 0);
		return false;
	}
}

internal class BunnyEarCactiSpaceholder : ModTile
{
	public override string Texture => base.Texture.Replace("Spaceholder", "");

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type] = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.StyleHorizontal = false;
		TileObjectData.newTile.RandomStyleRange = 1;
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand];
		TileObjectData.newTile.AnchorAlternateTiles = [Type, ModContent.TileType<BunnyEarCacti>()];

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.AlternateTile, 1, 1);
		TileObjectData.addAlternate(1);

		TileObjectData.addTile(Type);

		AddMapEntry(new Color(146, 188, 61));
		DustType = DustID.t_Cactus;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 2 : 6;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Cactus, Main.rand.Next(2, 4));
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;
}