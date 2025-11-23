using SpiritReforged.Content.Forest.Botanist.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Discoveries.Passes;

internal class ScarecrowDiscovery : Discovery
{
	public override string WorldGenName => "Scarecrow";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, List<Discovery> discoveries, ref bool afterIndex)
	{
		afterIndex = false;
		return passes.FindIndex(genpass => genpass.Name.Equals("Smooth World"));
	}

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int maxTries = 1000;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Scarecrow");
		int region = (int)(Main.maxTilesX * .45f);
		int tries = 0;

		int fieldSize = WorldGen.GetWorldSize() switch
		{
			1 => 8,
			2 => 12,
			_ => 5
		};

		while (tries < maxTries)
		{
			int x = WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(Main.maxTilesX - region, Main.maxTilesX) : WorldGen.genRand.Next(region);
			int y = WorldGen.remixWorldGen ? WorldGen.genRand.Next((int)(Main.maxTilesY / 1.5f), Main.maxTilesY - 200) : (int)(Main.worldSurface * .5f);

			WorldMethods.FindGround(x, ref y);

			var tile = Main.tile[x, y];
			if (tile.TileType == TileID.Dirt && tile.LiquidAmount == 0)
			{
				var area = new Rectangle(x - fieldSize, y - 2, fieldSize * 2 + 1, 3);

				if (TryGenArea(new Point16(x, y), fieldSize) && GenVars.structures.CanPlace(area, 2))
				{
					GenVars.structures.AddProtectedStructure(area, 2);
					return;
				}
			}

			tries++;
		}

		SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: " + WorldGenName);
	}

	private static bool TryGenArea(Point16 position, int width)
	{
		if (!WorldMethods.IsFlat(position, width, out int start, out int end) || WorldMethods.CloudsBelow(position.X, position.Y, out _))
			return false;

		int loopStart = position.X - width;
		int loopEnd = position.X + width + 1;

		for (int x = loopStart; x < loopEnd; x++)
		{
			float progress = ((float)x - loopStart) / (width * 2f);
			int y = (int)MathHelper.Lerp(start, end, progress);

			ClearAbove(x, y);
			FillBelow(x, y, progress);

			if (x == position.X)
				ScarecrowSlot.Generate(position.X, y - 1);
			else
				WorldGen.PlaceTile(x, y - 1, ModContent.TileType<Wheatgrass>(), true, style: Main.rand.Next(6));
		}

		return true;

		static void ClearAbove(int x, int floor)
		{
			int y = floor;
			WorldMethods.FindGround(x, ref y);

			while (y < floor)
			{
				Main.tile[x, y].ClearEverything();
				y++;
			}
		}

		static void FillBelow(int x, int floor, float depth)
		{
			int y = floor;
			int maxY = 3 + (int)(Math.Sin(depth * MathHelper.Pi) * 5);

			while (true)
			{
				var tile = Framing.GetTileSafely(x, y);

				tile.HasTile = true;
				tile.TileType = (y == floor) ? TileID.Grass : TileID.Dirt;

				if (++y > maxY + floor)
					break;
			}
		}
	}
}
