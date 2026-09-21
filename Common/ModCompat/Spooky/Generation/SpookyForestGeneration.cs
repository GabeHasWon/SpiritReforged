using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class SpookyForestGeneration : SpookyMicropass
{
	public override string WorldGenName => "Uncommon Spooky Forest";
	public override string ModifyType => "SpookyForest";
	public override string MatchPass => "Spooky Forest";

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (!CrossMod.Spooky.TryCall(out Vector2 BiomePosition, "BiomePositions", "SpookyBiomeCenter"))
			return;

		Point pos = BiomePosition.ToPoint();

		for (int X = pos.X - Main.maxTilesX / 12; X <= pos.X + Main.maxTilesX / 12; X++)
		{
			for (int Y = (int)Main.worldSurface; Y < Main.maxTilesY - 200; Y++)
			{
				Tile tile = Main.tile[X, Y];
				
				if (tile.TileType == SpookyTile("SpookyStone") || tile.TileType == SpookyTile("SpookyGrass") || tile.TileType == SpookyTile("SpookyGrassGreen") 
					|| tile.TileType == SpookyTile("MushroomMoss"))
				{
					if (WorldGen.genRand.NextBool(35))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, 24 + WorldGen.genRand.Next(0, 3));
				}
			}
		}

		static int SpookyTile(string name) => CrossMod.Spooky.Instance.Find<ModTile>(name).Type;
	}
}
