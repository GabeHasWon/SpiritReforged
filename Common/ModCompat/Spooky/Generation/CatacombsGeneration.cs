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

		for (int X = topTopLeft.X; X <= bottomBottomRight.X; X++)
		{
			for (int Y = (int)Main.worldSurface - 10; Y <= bottomBottomRight.Y; Y++)
			{
				Tile tile = Main.tile[X, Y];

				if (tile.WallType == upperWall)
				{
					if (WorldGen.genRand.NextBool(60))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, 3 + Main.rand.Next(0, 3));
				}
				else if (tile.WallType == lowerWall)
				{
					if (WorldGen.genRand.NextBool(30))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, Main.rand.Next(0, 3));
				}
			}
		}
	}
}
