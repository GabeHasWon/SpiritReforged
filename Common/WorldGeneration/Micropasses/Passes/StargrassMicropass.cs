using Terraria.WorldBuilding;
using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Content.Forest.Stargrass;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class StargrassMicropass : Micropass
{
	public override string WorldGenName => "Stargrass Patch";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Stargrass");

		float worldSize = Main.maxTilesX / 4200f;

		for (int i = 0; i < 4 * worldSize; i++)
		{
			int x = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
			int y = WorldGen.remixWorldGen ? WorldGen.genRand.Next(Main.maxTilesY / 2, Main.maxTilesY - 200) : (int)(Main.worldSurface * 0.35f);
			bool fail = false;

			while (!Main.tile[x, y].HasTile || Main.tile[x, y].TileType != TileID.Grass)
			{
				y++;

				if (y > Main.worldSurface)
				{
					fail = true;
					break;
				}
			}

			if (fail)
			{
				i--;
				continue;
			}

			SpreadStargrass(x, y);
		}
	}

	private static void SpreadStargrass(int x, int y)
	{
		int size = WorldGen.genRand.Next(12, 20);

		for (int i = x - size; i < x + size; ++i)
		{
			for (int j = y - size; j < y + size; ++j)
			{
				if (Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j)) < size * size)
					StargrassConversion.Convert(i, j);
			}
		}
	}
}
