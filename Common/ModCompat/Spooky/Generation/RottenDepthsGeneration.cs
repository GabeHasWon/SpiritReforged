using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class RottenDepthsGeneration : SpookyMicropass
{
	public override string WorldGenName => "Uncommon Rotten Depths";
	public override string ModifyType => "ZombieOcean";
	public override string MatchPass => "Rotten Depths";

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (!CrossMod.Spooky.TryCall(out Vector2 topLeft, "BiomePositions", "ZombieOceanTopLeft"))
			return;

		if (!CrossMod.Spooky.TryCall(out Vector2 bottomRight, "BiomePositions", "ZombieOceanBottomRight"))
			return;

		Point top = topLeft.ToPoint();
		Point bottom = bottomRight.ToPoint();
		int sand = CrossMod.Spooky.Find<ModTile>("OceanSand").Type;
		int bio = CrossMod.Spooky.Find<ModTile>("OceanBiomass").Type;

		for (int j = top.Y; j < bottom.Y; j++)
		{
			for (int i = top.X; i < bottom.X; i++)
			{
				if (WorldGen.InWorld(i, j, 25))
				{
					Tile tile = Main.tile[i, j];

					if (WorldGen.genRand.NextBool(25) && (tile.TileType == sand || tile.TileType == bio))
						WorldGen.PlaceObject(i, j - 1, ModContent.TileType<UncommonSpookyPots>(), true, 12 + Main.rand.Next(0, 3));
				}
			}
		}
	}
}
