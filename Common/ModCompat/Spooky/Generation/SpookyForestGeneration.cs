using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class SpookyForestGeneration : Micropass
{
	public override string WorldGenName => "Spooky Uncommon Pots";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => throw new NotImplementedException();

	private static int SpookyTile(string name) => CrossMod.Spooky.Instance.Find<ModTile>(name).Type;

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (CrossMod.Spooky.TryCall(out Vector2 BiomePosition, "BiomePositions", "SpookyForest"))
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
					if (WorldGen.genRand.NextBool(15))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, WorldGen.genRand.Next(0, 3));
				}
			}
		}
	}
}
