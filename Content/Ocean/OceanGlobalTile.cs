using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Ocean.NPCs.OceanSlime;
using SpiritReforged.Content.Ocean.Tiles;
using SpiritReforged.Content.Savanna.Tiles;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean;

public class OceanGlobalTile : GlobalTile
{
	public override void RandomUpdate(int i, int j, int type)
	{
		int[] sands = [TileID.Sand, TileID.Crimsand, TileID.Ebonsand, TileID.Pearlsand]; //All valid sands
		int[] woods = [TileID.WoodBlock, TileID.BorealWood, TileID.Ebonwood, TileID.DynastyWood, TileID.RichMahogany, TileID.PalmWood, TileID.Shadewood, TileID.WoodenBeam,
			AutoContent.ItemType<Driftwood>(), ModContent.TileType<Drywood>(), TileID.Pearlwood];

		bool inOcean = (i < Main.maxTilesX / 16 || i > Main.maxTilesX / 16 * 15) && j < (int)Main.worldSurface;
		bool inWorldBounds = i > 40 && i < Main.maxTilesX - 40;
		var above = Framing.GetTileSafely(i, j - 1);

		if (sands.Contains(type) && inOcean && inWorldBounds && !above.HasTile && !Framing.GetTileSafely(i, j).TopSlope)
		{
			if (above.LiquidAmount == 255) //water stuff
			{
				if (Main.rand.NextBool(35)) //Ocean kelp
					Placer.PlaceTile<OceanKelp>(i, j - 1).Send();
				else if (Main.rand.NextBool(60)) //1x2 kelp
					Placer.PlaceTile<OceanDecor1x2>(i, j - 1).Send();
				else if (Main.rand.NextBool(80)) //2x2 kelp
					Placer.PlaceTile<OceanDecor2x2>(i, j - 1).Send();
				else if (Main.rand.NextBool(90)) //2x3 kelp
					Placer.PlaceTile<OceanDecor2x3>(i, j - 1).Send();
			}
			else if (Main.rand.NextBool(6))
				SpawnSeagrass(i, j, 5);
		}

		if (inOcean && inWorldBounds && woods.Contains(type) && Main.rand.NextBool(48))
			SpawnMussels(i, j);
	}

	private static void SpawnSeagrass(int i, int j, int rangeFromGrass)
	{
		bool GrassInRange()
		{
			for (int x = 0; x < rangeFromGrass * 2; x++)
			{
				int scanX = i + x % rangeFromGrass * ((x >= rangeFromGrass - 1) ? -1 : 1); //Scan right then left

				if (Framing.GetTileSafely(scanX, j).TileType == TileID.Grass)
					return true;
			}

			return false;
		} //Checks whether grass is in range (left or right) of the tile at these coordinates

		if (GrassInRange())
			Placer.PlaceTile<Seagrass>(i, j - 1).Send();
	}

	private static void SpawnMussels(int i, int j)
	{
		const int limitRadius = 6;
		const int spawnLimit = 10;

		int type = ModContent.TileType<Mussel>();
		Point16[] offset = [new Point16(0, -1), new Point16(-1, 0), new Point16(1, 0), new Point16(0, 1)];
		var coords = new Point16(i, j) + offset[Main.rand.Next(4)];

		var current = Framing.GetTileSafely(coords);
		if (!current.HasTile && current.LiquidAmount > 155 && current.LiquidType == LiquidID.Water && WorldGen.CountNearBlocksTypes(i, j, limitRadius, spawnLimit, type) < spawnLimit)
		{
			Placer.PlaceTile<Mussel>(coords.X, coords.Y, Main.rand.Next(Mussel.StyleRange)).Send();
			return;
		}
	}

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || !fail)
			return;

		if (type == TileID.PalmTree && Main.rand.NextBool(10) && (i < WorldGen.beachDistance || i > Main.maxTilesX - WorldGen.beachDistance)) //palm trees at/near the beach
		{
			int x = i;
			int y = j;

			// Crawl upwards until an air tile is found
			while (y > 0 && Main.tile[x, y].HasTile)
				y--;

			if (NPC.CountNPCS(ModContent.NPCType<OceanSlime>()) < 1) //too many of these guys has to feel bad... right? feel free to remove if we want to troll players
				NPC.NewNPC(WorldGen.GetItemSource_FromTreeShake(i, j), x * 16, y * 16, ModContent.NPCType<OceanSlime>());
		}
	}
}
