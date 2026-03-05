using SpiritReforged.Content.Forest.Safekeeper;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Discoveries.Passes;

internal class SafekeeperDiscovery : Discovery
{
	public override string WorldGenName => "Safekeeper's Grave";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, List<Discovery> discoveries, ref bool afterIndex)
	{
		afterIndex = false;
		return passes.FindIndex(genpass => genpass.Name.Equals("Piles"));
	}

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int maxTries = 300;
		int tries = 0;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Discoveries");

		while (tries < maxTries)
		{
			int x = WorldGen.genRand.Next(GenVars.leftBeachEnd, GenVars.rightBeachStart);
			int y = (int)(Main.worldSurface * 0.35); //Sky height

			WorldMethods.FindGround(x, ref y);

			if (Surface(x, y) && Main.tile[x, y - 1].LiquidAmount != 255 && WorldMethods.AreaClear(x - 1, y - 2, 3, 2) && GenVars.structures.CanPlace(new Rectangle(x - 1, y - 2, 3, 3)))
			{
				WorldGen.PlaceTile(x, y - 1, TileID.Tombstones, true, true, style: WorldGen.genRand.Next(5));

				if (Main.tile[x, y - 1].TileType == TileID.Tombstones)
				{
					Sign.TextSign(Sign.ReadSign(x, y - 1), Language.GetTextValue("Mods.SpiritReforged.Misc.GraveText"));

					WorldGen.PlaceTile(x - 1, y - 1, ModContent.TileType<SkeletonHand>(), true, true, style: WorldGen.genRand.Next(3));

					GenVars.structures.AddProtectedStructure(new Rectangle(x - 1, y - 2, 3, 3));
					Main.spawnTileX = x;
					Main.spawnTileY = y;
					return;
				}
			}

			tries++;
		}

		SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: " + WorldGenName);

		static bool Surface(int x, int y)
		{
			for (int i = x - 1; i < x + 2; i++)
			{
				var tile = Framing.GetTileSafely(i, y);
				if (tile.TileType != TileID.Grass || !tile.HasTile || tile.Slope != SlopeType.Solid || tile.IsHalfBlock)
					return false;
			}

			return true;
		}
	}
}
