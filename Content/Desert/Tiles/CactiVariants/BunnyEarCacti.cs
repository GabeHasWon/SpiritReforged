using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.WorldGeneration.Noise;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.Tiles.CactiVariants;

internal class BunnyEarCacti : ModTile
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
			Rectangle src = new(0, 34 * segment.Style, 30, 32);
			Vector2 position = new Vector2(i + segment.XOffset, j - segment.Layer * 1.5f) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

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

		return false;
	}

	private void DrawTops(Vector2 position, Texture2D tex, BunnyEarCactiTE.Segment segment, SpriteBatch batch, float rotation, Vector2 origin, Point16 basePos)
	{
		int style = segment.MiniTops / 2;
		int count = segment.MiniTops / 6 + 1;
		var src = new Rectangle(32, 16 * style, 16, 16);

		batch.Draw(tex, position + origin + (rotation - MathHelper.PiOver2).ToRotationVector2() * 22, src, Color.White, rotation, src.Size() / 2f, 1f, SpriteEffects.None, 0);

		if (count == 2)
		{
			float noise = NoiseSystem.PerlinStatic(basePos.X, basePos.Y);
			style += (int)(noise * 30);
			style %= 6;
			src = new Rectangle(32, 16 * style, 16, 16);

			if (noise >= 0.5f)
			{
				Vector2 secondTopPos = position + origin + (rotation - MathHelper.PiOver4).ToRotationVector2() * 20;
				batch.Draw(tex, secondTopPos, src, Color.White, rotation + 1, src.Size() / 2f, 1f, SpriteEffects.None, 0);
			}
			else
			{
				Vector2 secondTopPos = position + origin + (rotation - MathHelper.PiOver4 * 3).ToRotationVector2() * 20;
				batch.Draw(tex, secondTopPos, src, Color.White, rotation - 1, src.Size() / 2f, 1f, SpriteEffects.None, 0);
			}
		}
	}

	public class BunnyEarCactiTE : ModTileEntity
	{
		/// <summary>
		/// Defines an individual segment of the bunny ear cactus.
		/// </summary>
		/// <param name="Style"></param>
		/// <param name="XOffset"></param>
		/// <param name="Layer"></param>
		/// <param name="Top"></param>
		/// <param name="Alt"></param>
		public readonly record struct Segment(int Style, int XOffset, float Length, float Rotation, int Layer, bool Top, bool Alt, int MiniTops);

		public List<Segment> Segments = [];
		public List<Segment> Tops = [];
		public int Max = 0;
		public int Layer = 0;
		public int AnchorId = 0;

		public override bool IsTileValidForEntity(int x, int y) => Main.tile[x, y].TileType == ModContent.TileType<BunnyEarCacti>();

		public override void Update()
		{
			if (Segments.Count < 1)
			{
				Segments.Add(new Segment(Main.rand.NextBool(2) ? 2 : 5, 0, 1f, 0, 0, false, false, -1));
				Max = Main.drunkWorld ? WorldGen.genRand.Next(2, 15) : WorldGen.genRand.Next(2, 4);
				Layer++;
			}

			if (WorldGen.genRand.NextBool(4) && Segments.Count < Max)
			{
				Tops.Clear();

				if (AnchorId >= Segments.Count) // Recalculate anchor ID if it was broken
					AnchorId = Segments.IndexOf(Segments.Where(x => !x.Alt).MaxBy(x => x.Layer));

				Segment segment = Segments[AnchorId];
				int baseOffset = segment.XOffset;
				bool addedSegment = AddSegment(segment, Position, baseOffset, out int xOffset);

				if (addedSegment)
				{
					AnchorId = Segments.Count - 1;

					if (baseOffset != xOffset && WorldGen.genRand.NextBool(4))
						AddSegment(segment, Position, baseOffset, out _, xOffset == baseOffset - 1 ? baseOffset + 1 : baseOffset - 1);

					Layer++;
				}
			}

			List<Segment> toRemove = [];

			foreach (Segment s in Segments)
			{
				Point16 pos = new(Position.X + s.XOffset, Position.Y - s.Layer * 2 + 1);
				Tile tile = Main.tile[pos];

				if (!tile.HasTile || tile.TileType != ModContent.TileType<BunnyEarCacti>() && tile.TileType != ModContent.TileType<BunnyEarCactiSpaceholder>())
					toRemove.Add(s);
			}

			foreach (Segment s in toRemove)
				Segments.Remove(s);
		}

		private bool AddSegment(Segment anchor, Point16 position, int baseOffset, out int xOffset, int overrideXOffset = -1)
		{
			xOffset = overrideXOffset == -1 ? baseOffset + GetRandomDirection() : overrideXOffset;
			int style = Main.rand.Next(2) + (WorldGen.genRand.NextBool(2) ? 0 : 3);
			Point16 placePos = new(position.X + xOffset, position.Y - Layer * 2 + 1);
			bool alt = overrideXOffset != -1;
			bool top = Segments.Count == Max - 1;
			float rotation = WorldGen.genRand.NextFloat(alt ? MathHelper.PiOver4 * 0.4f : 0f, MathHelper.PiOver4 * 0.7f);

			if (top)
				WorldGen.genRand.NextFloat(MathHelper.PiOver2 * -0.67f, MathHelper.PiOver2 * 0.67f);
			else
			{
				if (xOffset == anchor.XOffset)
					rotation = WorldGen.genRand.NextFloat(-0.2f, 0.2f);
				else if (xOffset < anchor.XOffset)
					rotation *= -1;

				if (alt)
					rotation *= 2;
			}

			bool canHaveMini = top || xOffset != anchor.XOffset && WorldGen.genRand.NextBool(3);
			var mainTop = new Segment(style, xOffset, WorldGen.genRand.NextFloat(1.1f, 1.5f), rotation, Layer, top, alt, canHaveMini ? WorldGen.genRand.Next(0, 12) : -1);

			Tile tile = Main.tile[placePos];

			if (tile.HasTile)
				return false;

			WorldGen.PlaceObject(position.X + xOffset, position.Y - Layer * 2 + 1, ModContent.TileType<BunnyEarCactiSpaceholder>(), true);
			tile = Main.tile[placePos];

			if (tile.HasTile && tile.TileType == ModContent.TileType<BunnyEarCactiSpaceholder>())
			{
				Segments.Add(mainTop);
				Tops.Add(mainTop);
				return true;
			}

			return false;
		}

		private static int GetRandomDirection()
		{
			if (WorldGen.genRand.NextBool(6))
				return 0;

			return WorldGen.genRand.NextBool() ? -1 : 1;
		}

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
		{
			var tileData = TileObjectData.GetTileData(type, style, alternate);
			int topLeftX = i - tileData.Origin.X;
			int topLeftY = j - tileData.Origin.Y;

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, topLeftX, topLeftY, tileData.Width, tileData.Height);
				NetMessage.SendData(MessageID.TileEntityPlacement, number: topLeftX, number2: topLeftY, number3: Type);
				return -1;
			}

			return Place(topLeftX, topLeftY);
		}

		public override void SaveData(TagCompound tag)
		{
			tag.Add("max", (byte)Max);
			tag.Add("layer", (byte)Layer);
			tag.Add("segmentCount", (byte)Segments.Count);

			for (int i = 0; i < Segments.Count; i++)
			{
				TagCompound segTag = [];
				segTag.Add("style", (byte)Segments[i].Style);
				segTag.Add("xOff", (byte)Segments[i].XOffset);
				segTag.Add("length", Segments[i].Length);
				segTag.Add("rot", Segments[i].Rotation);
				segTag.Add("layer", (byte)Segments[i].Layer);
				segTag.Add("top", Segments[i].Top);
				segTag.Add("alt", Segments[i].Alt);
				segTag.Add("miniTops", (byte)Segments[i].MiniTops);
				tag.Add("segments" + i, segTag);
			}
		}

		public override void LoadData(TagCompound tag)
		{
			Segments.Clear();
			Max = tag.GetByte("max");
			Layer = tag.GetByte("layer");
			int count = tag.GetByte("segmentCount");

			for (int i = 0; i < count; ++i)
			{
				TagCompound s = tag.GetCompound("segments" + i);
				Segments.Add(new Segment(s.GetByte("style"), s.GetByte("xOff"), s.GetFloat("length"), s.GetFloat("rot"), s.GetByte("layer"), s.GetBool("top"), 
					s.GetBool("alt"), s.GetByte("miniTops")));
			}
		}
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

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;
}