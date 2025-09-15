using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public static class ZigguratRooms
{
	public class BasicRoom(Rectangle bounds, Point origin = default) : GenRoom(origin)
	{
		protected readonly Rectangle _bounds = bounds;

		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / WorldGen.genRand.Next([6, 8, 10]), 14);

		public override void Create()
		{
			const int curveHeight = 3;
			Rectangle rectangleBounds = new(Bounds.Left, Bounds.Top + curveHeight, Bounds.Width, Bounds.Height - curveHeight);

			WorldUtils.Gen(new(rectangleBounds.Left, rectangleBounds.Top), new Shapes.Rectangle(rectangleBounds.Width, rectangleBounds.Height), new Actions.ClearTile());
			WorldUtils.Gen(new(rectangleBounds.X, rectangleBounds.Y - 1), new GenTypes.Curve(rectangleBounds.Width, curveHeight), new Actions.ClearTile());

			AddLinks();
		}

		/// <summary> Called after all possible hallways linking ziggurat rooms are placed and the structure is 'sandified'.<para/>
		/// Should be used to safely place furniture and check for consumed links in <see cref="GenRoom.Links"/>. </summary>
		public virtual void FinalPass() => WorldMethods.GenerateSquared(static (i, j) =>
		{
			if (WorldGen.genRand.NextBool(25) && WorldGen.SolidTile(i, j - 1))
				return Placer.PlaceTile(i, j, TileID.Banners, WorldGen.genRand.Next(4, 8)).success;

			return false;
		}, out _, Bounds);

		protected virtual void AddLinks()
		{
			Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
			Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));

			if (WorldGen.genRand.NextBool(4))
				Links.Add(new(new(Bounds.Center.X, Bounds.Bottom), Bottom));
		}
	}

	/*public class SandyRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		public override void FinalPass()
		{
			base.FinalPass();

			WorldMethods.Generate(static (i, j) =>
			{
				while (WorldGen.InWorld(i, j, 2) && !WorldGen.SolidTile(i, j))
					j++;

				if (!WorldGen.SolidTile(i, j - 1) && Main.tile[i, j].TileType != TileID.Sand)
				{
					WorldUtils.Gen(new(i, j), new Shapes.Mound(WorldGen.genRand.Next(5, 11), WorldGen.genRand.Next(2, 6)), new Actions.SetTileKeepWall(TileID.Sand));
					return true;
				}

				return false;
			}, 9, out _, Bounds, 10);
		}
	}*/

	public class DigsiteRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
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
	}

	public class EntranceRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		private int _exitSide;

		protected override void Initialize(out Point size) => size = new(_bounds.Width / 2 + 1, _bounds.Height - 4);

		public override void Create()
		{
			int bottom = Bounds.Bottom - 2;

			_exitSide = WorldGen.genRand.Next([-1, 1]);

			base.Create();

			if (_exitSide == -1)
				ZigguratBiome.BlockOut(new(Bounds.Left, bottom), new(_bounds.Left, bottom), 3);
			else if (_exitSide == 1)
				ZigguratBiome.BlockOut(new(Bounds.Right, bottom), new(_bounds.Right, bottom), 3);

			//Center indent
			int tailSquared = Bounds.Width / 6;
			WorldUtils.Gen(new(Bounds.Center.X - tailSquared - 1, Bounds.Bottom), new Shapes.Tail(tailSquared, new(0, tailSquared / 2 + 1)), new Actions.ClearTile());
			WorldUtils.Gen(new(Bounds.Center.X + tailSquared, Bounds.Bottom), new Shapes.Tail(tailSquared, new(0, tailSquared / 2 + 1)), new Actions.ClearTile());
			WorldUtils.Gen(new(Bounds.Center.X, Bounds.Bottom), new Shapes.Rectangle(new(-tailSquared, 0, tailSquared * 2, tailSquared / 2 + 1)), new Actions.ClearTile());

			PlaceColumn(new(Bounds.Center.X - tailSquared, Bounds.Bottom + 1));
			PlaceColumn(new(Bounds.Center.X + tailSquared, Bounds.Bottom + 1));

			static void PlaceColumn(Point origin)
			{
				while (WorldGen.InWorld(origin.X, origin.Y, 2) && !WorldGen.SolidTile(origin))
				{
					WorldGen.PlaceTile(origin.X, origin.Y, ModContent.TileType<RuinedSandstonePillar>(), true);
					WorldGen.PlaceTile(origin.X - 1, origin.Y, ModContent.TileType<RuinedSandstonePillar>(), true);
					origin.Y--;
				}
			}
		}

		public override void FinalPass() { }

		protected override void AddLinks()
		{
			Links.Add(new(new(Bounds.Center.X, Bounds.Bottom), Bottom));

			if (_exitSide == 1)
				Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
			else if (_exitSide == -1)
				Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));
		}
	}

	public class TreasureRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / 5 - 1, 20);

		public override void FinalPass()
		{
			WorldMethods.GenerateSquared(static (i, j) =>
			{
				if (WorldGen.genRand.NextBool(10) && WorldGen.SolidTile(i, j + 1))
				{
					PlaceCoinPile(i, j);
					return true;
				}

				return false;
			}, out _, Bounds);

			static void PlaceCoinPile(int i, int j)
			{
				int num4 = WorldGen.genRand.Next(1, 4);
				int num5 = (WorldGen.genRand.Next() % 2 == 0) ? 4 : (-(WorldGen.genRand.Next(6, 10) >> 1));

				for (int k = 0; k < num4; k++)
				{
					int num6 = WorldGen.genRand.Next(1, 3);
					for (int l = 0; l < num6; l++)
					{
						int x = i + num5 - k;
						int y = j - l;

						if (!Main.tile[x, y].HasTile)
							WorldGen.PlaceTile(x, y, TileID.GoldCoinPile, mute: true);
					}
				}
			}
		}
	}

	/*public class Connector(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
	{
		protected override void Initialize(out Point size) => size = new(ZigguratBiome.HallwayWidth + 2, ZigguratBiome.HallwayWidth + 2);

		public override void Create()
		{
			Rectangle rectangleBounds = new(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height);
			WorldUtils.Gen(new(rectangleBounds.Left, rectangleBounds.Top), new Shapes.Rectangle(rectangleBounds.Width, rectangleBounds.Height), new Actions.ClearTile());

			AddLinks();
		}

		protected override void AddLinks()
		{
			Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
			Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));
			Links.Add(new(new(Bounds.Center.X, Bounds.Top), Top));
		}
	}*/
}