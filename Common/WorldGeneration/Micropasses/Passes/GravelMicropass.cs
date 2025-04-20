using SpiritReforged.Content.Ocean.Hydrothermal.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class GravelMicropass : Micropass
{
	public override string WorldGenName => "Gravel";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Quick Cleanup")); //Generate after Quick Cleanup or contend with the dirt chunks.
	}

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int width = 2;
		const int height = 4;

		const int maxTries = 2000;
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Gravel");

		int count = 0;
		int countMax = 9;

		for (int i = 0; i < maxTries; i++)
		{
			int x = WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(Main.maxTilesX - WorldGen.beachDistance, Main.maxTilesX - 40) : WorldGen.genRand.Next(40, WorldGen.beachDistance);

			int y = WorldGen.genRand.Next((int)(Main.maxTilesY * 0.35f / 16f), (int)WorldGen.oceanLevel);
			while (Framing.GetTileSafely(x, y).LiquidAmount == 255)
				y++;

			if (Surface(x, y) && GenVars.structures.CanPlace(new Rectangle(x, y - height, height, width), 4) && WorldMethods.AreaClear(x, y - height, width, height) && WorldMethods.Submerged(x, y - height - 2, width, height + 2))
			{
				GenGravel(new Point16(x, y));

				if (++count >= countMax)
					return;

				i--;
			}
		}

		static bool Surface(int x, int y)
		{
			int type = TileID.Sand;
			for (int w = x; w < x + 2; w++)
			{
				var tile = Framing.GetTileSafely(w, y);
				if (!tile.HasTile || tile.TileType != type)
					return false;
			}

			return true;
		}
	}

	private static void GenGravel(Point16 position)
	{
		int x = position.X;
		int y = position.Y;

		int pillarHeight = WorldGen.genRand.Next(3);
		int rockCount = WorldGen.genRand.Next(3);

		WorldGen.OreRunner(x, y, WorldGen.genRand.Next(5, 10), WorldGen.genRand.Next(10, 15), (ushort)ModContent.TileType<Gravel>()); //Initial gravel patch

		WorldUtils.Gen(new Point(x, y - pillarHeight), new Shapes.Rectangle(2, pillarHeight + 1), Actions.Chain(new Actions.SetTile((ushort)ModContent.TileType<Gravel>()), new Actions.SetLiquid())); //Be careful with this

		WorldGen.PlaceTile(x, y - pillarHeight - 1, ModContent.TileType<HydrothermalVent>(), true, style: WorldGen.genRand.Next(8));

		for (int i = 0; i < rockCount; i++)
		{
			int offX = x + (Main.rand.NextBool() ? -WorldGen.genRand.Next(2, 5) : WorldGen.genRand.Next(2, 5));
			int offY = y;

			WorldMethods.FindGround(offX, ref offY);

			if (WorldGen.InWorld(offX, offY))
				SmallRock(offX, ref offY, WorldGen.genRand.Next(2, 4));
		}

		if (WorldGen.genRand.NextBool(4)) //Extension gravel patch
		{
			int offX = x + WorldGen.genRand.Next(-3, 3);
			int offY = y;

			MoveBelowGravel(offX, ref offY);

			WorldGen.OreRunner(offX, offY, WorldGen.genRand.Next(3, 8),
				WorldGen.genRand.Next(6, 13), (ushort)ModContent.TileType<Gravel>());
		}

		int magmaCount = WorldGen.genRand.Next(2, 5);
		for (int i = 0; i < magmaCount; i++) //Bottom magma patches
		{
			int offX = x + WorldGen.genRand.Next(-10, 10);
			int offY = y;

			MoveBelowGravel(offX, ref offY);

			if (WorldGen.SolidTile(offX, offY - 1)) //Only move back up to the gravel tile, presumably, if it's buried
				offY--;

			if (Main.tile[offX, offY].HasTile && Main.tile[offX, offY].TileType == ModContent.TileType<Gravel>())
				WorldGen.OreRunner(offX, offY, 4, 1, (ushort)ModContent.TileType<Magmastone>());
			else
				i--; //Try again
		}

		static void MoveBelowGravel(int x, ref int y)
		{
			while (Framing.GetTileSafely(x, y).TileType == ModContent.TileType<Gravel>())
				y++;
		}
	}

	private static void SmallRock(int x, ref int y, int size)
	{
		//Tiles we're allowed to modify/destroy
		int[] canTouch = [TileID.Sand, TileID.HardenedSand, ModContent.TileType<Gravel>()];

		if (!GenVars.structures.CanPlace(new Rectangle(x, y, size, size)))
			return;

		for (int i = 0; i < size * size; i++)
		{
			var p = new Point(x - i % size, y - i / size);
			if (Main.tile[p.X, p.Y].HasTile && !canTouch.Contains(Main.tile[p.X, p.Y].TileType))
				continue;

			if (WorldGen.CanKillTile(p.X, p.Y, WorldGen.SpecialKillTileContext.None))
				WorldGen.KillTile(p.X, p.Y, noItem: true);

			WorldGen.PlaceTile(p.X, p.Y, ModContent.TileType<Gravel>(), true);

			if (i >= size * (size - 1)) //Top layer slope logic
			{
				if (size == 2)
					Framing.GetTileSafely(p).IsHalfBlock = true;
				else if (i % size == 0)
					Framing.GetTileSafely(p).Slope = SlopeType.SlopeDownLeft;
				else if (i % size == size - 1)
					Framing.GetTileSafely(p).Slope = SlopeType.SlopeDownRight;
			}
		}

		y -= size - 1;
	}
}
