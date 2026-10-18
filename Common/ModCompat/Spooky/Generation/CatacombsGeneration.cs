using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class CatacombsGeneration : SpookyMicropass
{
	public override string WorldGenName => "Uncommon Catacombs";
	public override string ModifyType => "Catacombs";
	public override string MatchPass => "Creepy Catacombs";

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (!CrossMod.Spooky.TryCall(out Point16 topTopLeft, "BiomePositions", "CatacombUpperTopLeft"))
			return;

		if (!CrossMod.Spooky.TryCall(out Point16 bottomBottomRight, "BiomePositions", "CatacombLowerBottomRight"))
			return;

		int upperWall = CrossMod.Spooky.Find<ModWall>("CatacombBrickWall1").Type;
		int lowerWall = CrossMod.Spooky.Find<ModWall>("CatacombBrickWall2").Type;
		List<Point16> shovelPositions = [];

		for (int X = topTopLeft.X; X <= bottomBottomRight.X; X++)
		{
			for (int Y = (int)Main.worldSurface - 10; Y <= bottomBottomRight.Y; Y++)
			{
				Tile tile = Main.tile[X, Y];

				if (tile.WallType == upperWall)
				{
					if (WorldGen.genRand.NextBool(60))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, 3 + WorldGen.genRand.Next(0, 3));

					if (WorldGen.genRand.NextBool(10))
						shovelPositions.Add(new Point16(X, Y - 1));
				}
				else if (tile.WallType == lowerWall)
				{
					if (WorldGen.genRand.NextBool(30))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, WorldGen.genRand.Next(0, 3));
				}
			}
		}

		int count = (int)(Main.maxTilesX / 4200f) * 2;
		int overallTries = 0;

		for (int i = 0; i < count; ++i)
		{
			while (true)
			{
				Point16 pos = WorldGen.genRand.Next(shovelPositions);
				overallTries++;

				if (overallTries > 100_000)
					return;

				if (Placer.Check(pos.X, pos.Y, ModContent.TileType<ShovelTile>()).Place().success)
					break;
			}
		}
	}
}
