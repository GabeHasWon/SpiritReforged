using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Desert.Tiles.Chains;
using SpiritReforged.Content.Desert.Walls;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public static class ZigguratRooms
{
	public class BasicRoom(Rectangle bounds, Point origin = default) : GenRoom(origin)
	{
		protected readonly Rectangle _outerBounds = bounds;

		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / WorldGen.genRand.Next([8, 10]), 14);

		public override void AddLinks()
		{
			Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
			Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));
		}

		public override void Create()
		{
			CarveOut();

			PlaceColumn(new(Bounds.Left - 1, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right + 1, Bounds.Bottom - 1), 2);

			//Add decorations
			WorldMethods.GenerateSquared(static (i, j) =>
			{
				if (WorldGen.genRand.NextBool(25) && WorldGen.SolidTile(i, j - 1))
					return Placer.PlaceTile(i, j, TileID.Banners, WorldGen.genRand.Next(4, 8)).success;

				if (WorldGen.genRand.NextBool(45) && WorldGen.SolidTile(i, j - 1) && Placer.PlaceTile<GoldChainLoop>(i, j).success)
					ChainObjectSystem.AddObject(ModContent.GetInstance<GoldChainLoop>().Find(new(i, j), (byte)WorldGen.genRand.Next(3, 7)));

				return false;
			}, out _, Bounds);
		}

		public void CarveOut()
		{
			const int curveHeight = 3;
			WorldUtils.Gen(Bounds.Location, new Shapes.Rectangle(Bounds.Width, Bounds.Height), new Actions.Custom((x, y, args) =>
			{
				float progress = (x - Bounds.Left) / (Bounds.Width - 1f);
				int curve = (int)((1f - EaseFunction.EaseSine.Ease(progress)) * curveHeight);

				if (y - Bounds.Top > curve)
				{
					WorldUtils.ClearTile(x, y);
					Main.tile[x, y].LiquidAmount = 0;

					return true;
				}

				return false;
			}));
		}

		/// <summary> Places upward-expanding <see cref="RuinedSandstonePillar"/> tiles. </summary>
		public static void PlaceColumn(Point origin, int width)
		{
			while (WorldGen.InWorld(origin.X, origin.Y, 2) && !WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(origin)))
			{
				for (int x = 0; x < width; x++)
					WorldGen.PlaceTile(origin.X - width / 2 + x, origin.Y, ModContent.TileType<RuinedSandstonePillar>(), true);

				origin.Y--;
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

	public class EntranceRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		protected override void Initialize(out Point size) => size = new(_outerBounds.Width - 10, _outerBounds.Height - 4);

		public override void AddLinks() => Links.Add(new(new(Bounds.Center.X, Bounds.Bottom + 2), Bottom));

		public override void Create()
		{
			const int entranceHeight = 8;

			base.Create();

			bool leftClear = WorldMethods.AreaCount(_outerBounds.Left - 1, Bounds.Bottom - entranceHeight, 1, entranceHeight, false) <= 4;
			bool rightClear = WorldMethods.AreaCount(_outerBounds.Right, Bounds.Bottom - entranceHeight, 1, entranceHeight, false) <= 4;

			if (leftClear)
				WorldUtils.Gen(new(_outerBounds.Left, Bounds.Bottom - entranceHeight), new Shapes.Rectangle(5, entranceHeight), new Actions.ClearTile());

			if (rightClear)
				WorldUtils.Gen(new(Bounds.Right, Bounds.Bottom - entranceHeight), new Shapes.Rectangle(5, entranceHeight), new Actions.ClearTile());

			WorldUtils.Gen(new(Bounds.Left - 2, Bounds.Bottom - 4), new Shapes.Rectangle(Bounds.Width + 4, 4), new Actions.PlaceWall((ushort)RedSandstoneBrickWall.UnsafeType));

			PlaceColumn(new(Bounds.Left - 2, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right + 2, Bounds.Bottom - 1), 2);

			PlaceColumn(new(Bounds.Left + 6, Bounds.Bottom - 1), 2);
			PlaceColumn(new(Bounds.Right - 6, Bounds.Bottom - 1), 2);
		}
	}

	public class TreasureRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / 5, 20);

		public override void AddLinks()
		{
			Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
			Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));
		}

		public override void Create()
		{
			base.Create();

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
}