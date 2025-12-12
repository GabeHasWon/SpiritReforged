using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Desert.Walls;
using System.Linq;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public static class ZigguratRooms
{
	public readonly struct RoomNoise(float smoothing)
	{
		public readonly float Get(int x, int y) => Noise.NoiseSystem.PerlinStatic(x / smoothing, y / smoothing) * 2f;
		public readonly Point Modified(Point pt) => pt + new Point(0, (int)Get(pt.X, pt.Y));
	}

	public class BasicRoom(Rectangle bounds, RoomNoise noise, Point origin = default) : GenRoom(origin)
	{
		protected readonly Rectangle _outerBounds = bounds;
		protected readonly RoomNoise _noise = noise;

		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / WorldGen.genRand.Next([8, 10]), 14);

		public override void AddLinks()
		{
			Point left = new(Bounds.Left - 1, Bounds.Bottom - 2);
			Point right = new(Bounds.Right, Bounds.Bottom - 2);

			Links.Add(new(_noise.Modified(left), Left));
			Links.Add(new(_noise.Modified(right), Right));
		}

		public override void Create()
		{
			const int tertiaryHeight = 2;

			CarveOut(out ShapeData data);

			WorldUtils.Gen(Bounds.Location, new ModShapes.OuterOutline(data), Actions.Chain(
				new Modifiers.RectangleMask(-WorldGen.genRand.Next(2), Bounds.Width + 1, -1, Bounds.Height - 1),
				new Modifiers.IsTouchingAir(),
				new Modifiers.IsSolid(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneSlab>())
			));

			if (WorldGen.genRand.NextBool())
			{
				WorldUtils.Gen(Bounds.Location, new ModShapes.All(data), Actions.Chain(
					new Modifiers.Offset(0, 1),
					new Modifiers.Dither(),
					new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
					new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneSlab>())
				));
			}

			PlaceColumn(new(Bounds.Left - 1, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right + 1, Bounds.Bottom - 1), 2);

			if (WorldGen.genRand.NextBool())
				WorldGen.PlaceTile(Bounds.Left + 1, Bounds.Bottom - 3, ModContent.TileType<ZigguratTorch>());
			if (WorldGen.genRand.NextBool())
				WorldGen.PlaceTile(Bounds.Right - 2, Bounds.Bottom - 3, ModContent.TileType<ZigguratTorch>());

			WorldUtils.Gen(new(Bounds.Left - 2, Bounds.Bottom - (tertiaryHeight - 1)), new Shapes.Rectangle(Bounds.Width + 4, tertiaryHeight), Actions.Chain(
				new Actions.PlaceWall((ushort)RedSandstoneBrickWall.UnsafeType),
				new Modifiers.Expand(1),
				new Modifiers.Dither(0.8),
				new Modifiers.OnlyWalls(WallID.Sandstone),
				new Actions.PlaceWall((ushort)RedSandstoneBrickCrackedWall.UnsafeType)));
		}

		public void CarveOut(out ShapeData data)
		{
			const int curveHeight = 3;

			ShapeData newData = new();
			WorldUtils.Gen(Bounds.Location, new Shapes.Rectangle(Bounds.Width, Bounds.Height + 1), new Actions.Custom((x, y, args) =>
			{
				float progress = (x - Bounds.Left) / (Bounds.Width - 1f);
				int curve = (int)((1f - EaseFunction.EaseSine.Ease(progress)) * curveHeight);

				if (y - Bounds.Top > curve && y - Bounds.Bottom < _noise.Get(x, y))
				{
					WorldUtils.ClearTile(x, y);
					Main.tile[x, y].LiquidAmount = 0;

					return true;
				}

				return false;
			}).Output(newData));

			data = newData;
		}

		/// <summary> Places upward-expanding <see cref="RuinedSandstonePillar"/> tiles. </summary>
		public void PlaceColumn(Point origin, int width, int height = 0)
		{
			bool setGround = false;
			int count = 0;

			if (height == 0)
				height = Bounds.Height;

			while (count++ < height && WorldGen.InWorld(origin.X, origin.Y, 2))
			{
				for (int x = 0; x < width; x++)
				{
					int i = origin.X - width / 2 + x;
					int j = origin.Y;
					Tile tile = Main.tile[i, j];

					if (!WorldGen.SolidOrSlopedTile(i, j))
						WorldGen.PlaceTile(i, j, ModContent.TileType<RuinedSandstonePillar>(), true);
					else
						tile.Slope = SlopeType.Solid;

					if (!setGround)
						WorldGen.PlaceTile(i, j + 1, ModContent.TileType<RedSandstoneBrick>(), true);
				}

				origin.Y--;
				setGround = true;
			}
		}
	}

	/*public class DigsiteRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		public override void FinalPass()
		{
			base.FinalPass();

			WorldMethods.GenerateSquared((i, j) =>
			{
				var tile = Main.tile[i, j];

				if ((i == Bounds.Left + 3 || i == Bounds.Right - 4) && !tile.HasTile)
					WorldGen.PlaceTile(i, j, TileID.WoodenBeam, true);

				if (i == Bounds.Left + 4 && !tile.HasTile)
					WorldGen.PlaceTile(i, j, TileID.Rope, true);

				if (j == Bounds.Top + 5)
					WorldGen.PlaceTile(i, j, TileID.Platforms, true);

				if (WorldGen.SolidTile(i - 1, j + 1, true) && WorldGen.genRand.NextBool(15))
					WorldGen.PlaceTile(i - 1, j, TileID.Campfire, true);

				return false;
			}, out _, Bounds);
		}
	}*/

	public class EntranceRoom(Rectangle bounds, EntranceRoom.StyleID style, RoomNoise noise, Point origin = default) : BasicRoom(bounds, noise, origin)
	{
		public enum StyleID
		{
			Blank,
			Large,
			Split
		}

		public readonly StyleID style = style;

		protected override void Initialize(out Point size) => size = new(_outerBounds.Width - 10, _outerBounds.Height - 4);

		public override void AddLinks() => Links.Add(new(new(Bounds.Center.X, Bounds.Bottom), Bottom));

		public override void Create()
		{
			const int entranceHeight = 8;
			const int tertiaryHeight = 3;

			base.Create();

			bool leftClear = WorldMethods.AreaCount(_outerBounds.Left - 1, Bounds.Bottom - entranceHeight, 1, entranceHeight, false) <= 4;
			bool rightClear = WorldMethods.AreaCount(_outerBounds.Right, Bounds.Bottom - entranceHeight, 1, entranceHeight, false) <= 4;

			if (leftClear)
				WorldUtils.Gen(new(_outerBounds.Left, Bounds.Bottom - entranceHeight), new Shapes.Rectangle(5, entranceHeight), new Actions.ClearTile());

			if (rightClear)
				WorldUtils.Gen(new(Bounds.Right, Bounds.Bottom - entranceHeight), new Shapes.Rectangle(5, entranceHeight), new Actions.ClearTile());

			int columnMiddleLeft = Bounds.Left + 6;
			int columnMiddleRight = Bounds.Right - 6;

			WorldUtils.Gen(new(Bounds.Left - 2, Bounds.Bottom - (tertiaryHeight - 1)), new Shapes.Rectangle(Bounds.Width + 4, tertiaryHeight), Actions.Chain(
				new Actions.PlaceWall((ushort)RedSandstoneBrickWall.UnsafeType),
				new Modifiers.Expand(1),
				new Modifiers.Dither(0.8),
				new Modifiers.OnlyWalls(WallID.Sandstone),
				new Actions.PlaceWall((ushort)RedSandstoneBrickCrackedWall.UnsafeType)));

			if (style == StyleID.Large)
			{
				const int grateHeight = 8;
				Rectangle grateArea = new(columnMiddleLeft, Bounds.Center.Y - grateHeight / 2 + 1, columnMiddleRight - columnMiddleLeft, grateHeight);

				WorldUtils.Gen(grateArea.Location, new Shapes.Rectangle(grateArea.Width, grateArea.Height), new Actions.PlaceWall((ushort)BronzeGrate.UnsafeType));
				WorldUtils.Gen(WorldGen.genRand.NextVector2FromRectangle(grateArea).ToPoint(), new Shapes.Circle(WorldGen.genRand.Next(2, 5)), Actions.Chain(
					new Modifiers.RadialDither(2, 3),
					new Modifiers.OnlyWalls((ushort)BronzeGrate.UnsafeType),
					new Actions.ClearWall()));

				WorldUtils.Gen(new(grateArea.X, grateArea.Y + grateArea.Height), new Shapes.Rectangle(grateArea.Width, 1), new Actions.PlaceTile((ushort)ModContent.TileType<BronzePlatform>()));
			}
			else if (style == StyleID.Split)
			{
				const int grateHeight = 10;
				for (int i = 0; i < 2; i++)
				{
					Rectangle grateArea = (i == 0) ? new(Bounds.Left, Bounds.Center.Y - grateHeight / 2 + 1, columnMiddleLeft - Bounds.Left, grateHeight)
						: new(columnMiddleRight, Bounds.Center.Y - grateHeight / 2 + 1, Bounds.Right - columnMiddleRight, grateHeight);

					WorldUtils.Gen(grateArea.Location, new Shapes.Rectangle(grateArea.Width, grateArea.Height), new Actions.PlaceWall((ushort)BronzeGrate.UnsafeType));
					WorldUtils.Gen(WorldGen.genRand.NextVector2FromRectangle(grateArea).ToPoint(), new Shapes.Circle(WorldGen.genRand.Next(2, 5)), Actions.Chain(
						new Modifiers.RadialDither(2, 3),
						new Modifiers.OnlyWalls((ushort)BronzeGrate.UnsafeType),
						new Actions.ClearWall()));

					WorldUtils.Gen(new(grateArea.X, grateArea.Y + grateArea.Height), new Shapes.Rectangle(grateArea.Width, 1), new Actions.PlaceTile((ushort)ModContent.TileType<BronzePlatform>()));
				}
			}

			PlaceColumn(new(Bounds.Left - 2, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right + 2, Bounds.Bottom - 1), 2);

			PlaceColumn(new(columnMiddleLeft, Bounds.Bottom - 1), 2);
			PlaceColumn(new(columnMiddleRight, Bounds.Bottom - 1), 2);
		}
	}

	public class StorageRoom(Rectangle bounds, RoomNoise noise, Point origin = default) : BasicRoom(bounds, noise, origin)
	{
		public override void Create()
		{
			const int overhangWidth = 6;

			bool leftOpen = Links.Any(static x => x.consumed && x.Direction == Left);
			bool rightOpen = Links.Any(static x => x.consumed && x.Direction == Right);

			CarveOut(out _);

			PlaceColumn(new(Bounds.Left - 1, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right + 1, Bounds.Bottom - 1), 2);

			WorldUtils.Gen(new(Bounds.Left - 1, Bounds.Top - 1), new Shapes.Rectangle(Bounds.Width + 2, Bounds.Height + 2), new Actions.PlaceWall((ushort)BronzePlatingWall.UnsafeType));
			WorldUtils.Gen(new(Bounds.Left - 1, Bounds.Bottom - 4), new Shapes.Rectangle(Bounds.Width + 2, 4), new Actions.PlaceWall((ushort)RedSandstoneBrickWall.UnsafeType));

			WorldUtils.Gen(new(Bounds.Left, Bounds.Bottom - 6), new Shapes.Rectangle(Bounds.Width, 1), new Actions.PlaceTile((ushort)ModContent.TileType<BronzePlatform>()));

			if (leftOpen)
			{
				WorldUtils.Gen(new(Bounds.Left, Bounds.Bottom - 6), new Shapes.Rectangle(overhangWidth, 2), new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrick>()));
			}
			
			if (rightOpen)
			{
				WorldUtils.Gen(new(Bounds.Right - overhangWidth, Bounds.Bottom - 6), new Shapes.Rectangle(overhangWidth, 2), new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrick>()));
			}

			WorldUtils.Gen(new(Bounds.Left, Bounds.Bottom), new Shapes.Rectangle(Bounds.Width, 1), new Actions.Custom(static (i, j, args) =>
			{
				Tile tile = Main.tile[i, j];
				Tile aboveTile = Main.tile[i, j - 1];

				if (!aboveTile.HasTileType(ModContent.TileType<RuinedSandstonePillar>()))
					tile.ResetToType((ushort)ModContent.TileType<BronzePlating>());

				return false;
			})); //Bronze flooring

			WorldMethods.GenerateSquared(static (i, j) =>
			{
				if (WorldGen.genRand.NextBool(3) && !WorldGen.SolidTile(i, j) && WorldGen.SolidTile2(i, j + 1))
				{
					int type = WorldGen.genRand.NextBool(7) ? ModContent.TileType<LapisPots>() : ModContent.TileType<BronzePots>();
					Placer.Check(i, j, type).IsClear().Place();
				}

				return false;
			}, out _, Bounds); //Add decorations
		}
	}

	public class TreasureRoom(Rectangle bounds, RoomNoise noise, Point origin = default) : BasicRoom(bounds, noise, origin)
	{
		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / 5, 20);

		public override void Create()
		{
			base.Create();

			WorldUtils.Gen(new(Bounds.Left, Bounds.Bottom), new Shapes.Rectangle(Bounds.Width, 1), new Actions.SetTile((ushort)ModContent.TileType<CarvedLapis>()));

			int width = (int)(Bounds.Width / 1.5f);
			WorldUtils.Gen(new Point(Bounds.Center.X, Bounds.Bottom), new Shapes.Tail(width, new(0, -width)), Actions.Chain(
				new Modifiers.RectangleMask(-width - 1, width + 1, -3, 0),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrick>())
			));

			PlaceColumn(new(Bounds.Left - 1, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right + 1, Bounds.Bottom - 1), 2);

			PlaceColumn(new(Bounds.Left + 10, Bounds.Bottom - 4), 2);
			PlaceColumn(new(Bounds.Right - 11, Bounds.Bottom - 4), 2);

			WorldUtils.Gen(new(Bounds.Left - 1, Bounds.Top), new Shapes.Rectangle(11, Bounds.Height), new Actions.PlaceWall((ushort)CarvedLapisWall.UnsafeType));
			WorldUtils.Gen(new(Bounds.Right - 11, Bounds.Top), new Shapes.Rectangle(12, Bounds.Height), new Actions.PlaceWall((ushort)CarvedLapisWall.UnsafeType));

			//Fill with coin piles
			WorldMethods.GenerateSquared((i, j) =>
			{
				float noise = Noise.NoiseSystem.PerlinStatic(i, j) + 2;

				if (Bounds.Height - (j - Bounds.Top) < noise && !WorldGen.SolidTile3(i, j))
					WorldGen.PlaceTile(i, j, TileID.GoldCoinPile, true);

				return false;
			}, out _, Bounds);
		}
	}

	public class LibraryRoom(Rectangle bounds, RoomNoise noise, Point origin = default) : BasicRoom(bounds, noise, origin)
	{
		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / WorldGen.genRand.NextFromList(6, 8), 14);

		public override void Create()
		{
			base.Create();

			int count = WorldGen.genRand.Next(4, 7);
			for (int i = 0; i < count; i++)
			{
				int width = WorldGen.genRand.Next(3, 5);
				int height = WorldGen.genRand.Next(3, 9);

				WorldUtils.Gen(new Point(WorldGen.genRand.Next(Bounds.Left, Bounds.Right - width), Bounds.Bottom - height + 1), new Shapes.Rectangle(width, height), Actions.Chain(
					new Actions.PlaceTile((ushort)ModContent.TileType<TallSandstoneShelf>()),
					new Actions.Custom((i, j, args) =>
					{
						if (!Framing.GetTileSafely(i, j - 1).HasTile && WorldGen.genRand.NextBool(4))
							Placer.PlaceTile<AncientBooks>(i, j - 1);

						return true;
					})
				));
			}
		}
	}
}