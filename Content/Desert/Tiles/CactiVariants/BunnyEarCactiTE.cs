using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Noise;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.Tiles.CactiVariants;

internal partial class BunnyEarCacti
{
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

			if (WorldGen.genRand.NextBool(20) && Segments.Count < Max)
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
			{
				Segments.Remove(s);
				
				if (SegmentHasPear(out int count, s))
				{
					Point pos = new((Position.X + s.XOffset) * 16, (Position.Y - s.Layer) * 16);
					Item.NewItem(new EntitySource_TileBreak(Position.X + s.XOffset, Position.Y - s.Layer), new Rectangle(pos.X, pos.Y, 28, 28), ItemID.PinkPricklyPear, count);
				}
			}
		}

		public override void OnKill()
		{
			foreach (Segment s in Segments)
			{
				if (SegmentHasPear(out int count, s))
				{
					Point pos = new((Position.X + s.XOffset) * 16, (Position.Y - s.Layer) * 16);
					Item.NewItem(new EntitySource_TileBreak(Position.X + s.XOffset, Position.Y - s.Layer), new Rectangle(pos.X, pos.Y, 28, 28), ItemID.PinkPricklyPear, count);
				}
			}
		}

		private bool AddSegment(Segment anchor, Point16 position, int baseOffset, out int xOffset, int overrideXOffset = -1)
		{
			xOffset = overrideXOffset == -1 ? baseOffset + GetRandomDirection() : overrideXOffset;
			int style = Main.rand.Next(2) + (WorldGen.genRand.NextBool(2) ? 0 : 3);
			Point16 placePos = new(position.X + xOffset, position.Y - Layer * 2 + 1);
			bool alt = overrideXOffset != -1 && !WorldGen.genRand.NextBool(4);
			bool top = Segments.Count == Max - 1;
			float rotation = WorldGen.genRand.NextFloat(alt ? MathHelper.PiOver4 * 0.4f : 0f, MathHelper.PiOver4 * 0.7f);

			if (xOffset == anchor.XOffset)
				rotation = WorldGen.genRand.NextFloat(-0.2f, 0.2f);
			else if (xOffset < anchor.XOffset)
				rotation *= -1;

			if (alt)
				rotation *= 2;
		
			bool canHaveMini = top || xOffset != anchor.XOffset && WorldGen.genRand.NextBool(3);
			var mainTop = new Segment(style, xOffset, WorldGen.genRand.NextFloat(1.1f, 1.5f), rotation, Layer, top, alt, canHaveMini ? WorldGen.genRand.Next(0, 12) : -1);

			Tile tile = Main.tile[placePos];

			if (tile.HasTile)
				return false;

			(int i, int j) = (position.X + xOffset, position.Y - Layer * 2 + 1);
			var attempt = Placer.Check(i, j, ModContent.TileType<BunnyEarCactiSpaceholder>());

			if (attempt.success)
			{
				attempt.Place().Send();
				Segments.Add(mainTop);
				Tops.Add(mainTop);

				NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID);
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
				segTag.Add("xOff", (short)Segments[i].XOffset);
				segTag.Add("length", Segments[i].Length);
				segTag.Add("rot", Segments[i].Rotation);
				segTag.Add("layer", (byte)Segments[i].Layer);
				segTag.Add("top", Segments[i].Top);
				segTag.Add("alt", Segments[i].Alt);
				segTag.Add("miniTops", (short)Segments[i].MiniTops);
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
				var seg = new Segment(s.GetByte("style"), s.GetShort("xOff"), s.GetFloat("length"), s.GetFloat("rot"), s.GetByte("layer"), s.GetBool("top"),
					s.GetBool("alt"), s.GetShort("miniTops"));
				Segments.Add(seg);
			}
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write((byte)Max);
			writer.Write((byte)Layer);
			writer.Write((byte)Segments.Count);

			for (int i = 0; i < Segments.Count; i++)
			{
				var seg = Segments[i];
				writer.Write((byte)seg.Style);
				writer.Write((byte)seg.XOffset);
				writer.Write((Half)seg.Length);
				writer.Write((Half)seg.Rotation);
				writer.Write((byte)seg.Layer);
				writer.Write(seg.Top);
				writer.Write(seg.Alt);
				writer.Write((short)seg.MiniTops);
			}
		}

		public override void NetReceive(BinaryReader reader)
		{
			Segments.Clear();
			Max = reader.ReadByte();
			Layer = reader.ReadByte();

			int count = reader.ReadByte();

			for (int i = 0; i < count; ++i)
			{
				byte style = reader.ReadByte();
				byte offset = reader.ReadByte();
				float length = (float)reader.ReadHalf();
				float rotation = (float)reader.ReadHalf();
				byte layer = reader.ReadByte();
				bool top = reader.ReadBoolean();
				bool alt = reader.ReadBoolean();
				short miniTops = reader.ReadInt16();

				Segments.Add(new Segment(style, offset, length, rotation, layer, top, alt, miniTops));
			}
		}

		private bool SegmentHasPear(out int pears, Segment seg)
		{
			pears = 0;

			if (seg.MiniTops != -1)
			{
				int style = seg.MiniTops / 2;
				int count = seg.MiniTops / 6 + 1;

				if (style == 5)
					pears++;

				if (count == 2)
				{
					float noise = NoiseSystem.PerlinStatic(Position.X, Position.Y);
					style += (int)MathF.Abs(noise * 30);
					style %= 6;

					if (style == 5)
						pears++;
				}
			}

			return pears > 0;
		}
	}
}
