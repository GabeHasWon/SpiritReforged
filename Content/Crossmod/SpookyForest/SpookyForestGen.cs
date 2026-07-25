using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Content.Forest.Stargrass.Items;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Crossmod.SpookyForest;

internal class SpookyForestGen : Micropass
{
	public override string WorldGenName => "Spirit Halloween: Spooky Forest";

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;
	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Guide");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		List<Point16> positions = [];
		CrossMod.Spooky.TryFind("SpookyGrass", out ModTile orange);
		CrossMod.Spooky.TryFind("SpookyGrassGreen", out ModTile green);

		for (int i = WorldGen.beachDistance; i < Main.maxTilesX - WorldGen.beachDistance; ++i)
		{
			for (int j = (int)(Main.worldSurface * 0.45); j < Main.worldSurface; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (!tile.HasTile)
					continue;

				if (tile.TileType == orange.Type || tile.TileType == green.Type)
					positions.Add(new Point16(i, j));
			}
		}

		int repeats = 0;

		while (true)
		{
			Point16 pos = WorldGen.genRand.Next(positions);
			WorldGen.PlaceObject(pos.X, pos.Y - 1, (ushort)ModContent.TileType<PumpkinPailTile>(), true, WorldGen.genRand.Next(3));
			Tile tile = Main.tile[pos.X, pos.Y - 1];

			if (tile.HasTile && tile.TileType == ModContent.TileType<PumpkinPailTile>())
				break;

			repeats++;

			if (repeats > 10_000)
				break;
		}

		float reps = Main.maxTilesX / 4200f * 3;

		for (int i = 0; i < reps; ++i)
		{
			Point16 pos = WorldGen.genRand.Next(positions);
			int size = WorldGen.genRand.Next(40, 71);
			WorldGen.Convert(pos.X, pos.Y, StarConversion.ConversionType, size, true, true);
		}
	}
}
