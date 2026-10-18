using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class NoseTempleGeneration : SpookyMicropass
{
	public override string WorldGenName => "Uncommon Nose Temple";
	public override string ModifyType => "SpookyHell";
	public override string MatchPass => "Nose Cultist Dungeon";

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (!CrossMod.Spooky.TryCall(out Point16 topLeft, "BiomePositions", "NoseTempleLeftmostPosition"))
			return;

		if (!CrossMod.Spooky.TryCall(out Point16 bottomRight, "BiomePositions", "NoseTempleRightmostPosition"))
			return;

		int originalY = topLeft.Y;
		topLeft = new Point16(topLeft.X, Main.maxTilesY - 150);
		bottomRight = new Point16(bottomRight.X, Main.maxTilesY - 40);
		HashSet<int> walls = [Wall("NoseTempleWallPurple"), Wall("NoseTempleFancyWallPurple"), Wall("NoseTempleWallBGPurple"), Wall("NoseTempleWallGray"), 
			Wall("NoseTempleFancyWallGray"), Wall("NoseTempleWallBGGray"), Wall("NoseTempleWallRed"), Wall("NoseTempleFancyWallRed"), Wall("NoseTempleWallBGRed"), 
			Wall("NoseTempleWallGreen"), Wall("NoseTempleFancyWallGreen"), Wall("NoseTempleWallBGGreen")];

		for (int X = topLeft.X; X <= bottomRight.X; X++)
		{
			for (int Y = topLeft.Y; Y < bottomRight.Y; Y++)
			{
				if (WorldGen.InWorld(X, Y))
				{
					Tile tile = Main.tile[X, Y];

					if (walls.Contains(tile.WallType))
					{
						int PotChance = Y >= originalY + 45 ? 150 : 50;

						if (WorldGen.genRand.NextBool(PotChance))
							WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, 18 + Main.rand.Next(0, 3));
					}
				}
			}
		}

		static int Wall(string name) => CrossMod.Spooky.Find<ModWall>(name).Type;
	}
}