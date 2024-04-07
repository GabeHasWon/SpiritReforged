﻿using SpiritReforged.Content.Ocean.Tiles;
using System.Linq;

namespace SpiritReforged.Content.Ocean;

public class OceanGlobalTile : GlobalTile
{
	public override void RandomUpdate(int i, int j, int type)
	{
		int[] sands = [TileID.Sand, TileID.Crimsand, TileID.Ebonsand]; //All valid sands
		int[] woods = [TileID.WoodBlock, TileID.BorealWood, TileID.Ebonwood, TileID.DynastyWood, TileID.RichMahogany, TileID.PalmWood, TileID.Shadewood, TileID.WoodenBeam, 
			ModContent.TileType<DriftwoodTile>(), TileID.Pearlwood];

		bool inOcean = (i < Main.maxTilesX / 16 || i > Main.maxTilesX / 16 * 15) && j < (int)Main.worldSurface; //Might need adjustment; don't know if this will be exclusively in the ocean
		bool inWorldBounds = i > 40 && i < Main.maxTilesX - 40;

		if (sands.Contains(type) && inOcean && inWorldBounds && !Framing.GetTileSafely(i, j - 1).HasTile && !Framing.GetTileSafely(i, j).TopSlope) //woo
		{
			if (Framing.GetTileSafely(i, j - 1).LiquidAmount > 200) //water stuff
			{
				if (Main.rand.NextBool(25))
					WorldGen.PlaceTile(i, j - 1, ModContent.TileType<OceanKelp>()); //Kelp spawning

				bool openSpace = !Framing.GetTileSafely(i, j - 2).HasTile;
				if (openSpace && Main.rand.NextBool(40)) //1x2 kelp
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Kelp1x2>());

				openSpace = !Framing.GetTileSafely(i + 1, j - 1).HasTile && !Framing.GetTileSafely(i + 1, j - 2).HasTile && !Framing.GetTileSafely(i, j - 2).HasTile;
				if (openSpace && Framing.GetTileSafely(i + 1, j).HasTile && Main.tileSolid[Framing.GetTileSafely(i + 1, j).TileType] && Framing.GetTileSafely(i + 1, j).TopSlope && Main.rand.NextBool(80)) //2x2 kelp
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Kelp2x2>());

				openSpace = !Framing.GetTileSafely(i + 1, j - 1).HasTile && !Framing.GetTileSafely(i + 1, j - 2).HasTile && !Framing.GetTileSafely(i, j - 2).HasTile && !Framing.GetTileSafely(i + 1, j - 3).HasTile && !Framing.GetTileSafely(i, j - 3).HasTile;
				if (openSpace && Framing.GetTileSafely(i + 1, j).HasTile && Main.tileSolid[Framing.GetTileSafely(i + 1, j).TileType] && Framing.GetTileSafely(i + 1, j).TopSlope && Main.rand.NextBool(90)) //2x3 kelp
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Kelp2x3>());
			}
			else
				if (Main.rand.NextBool(6))
					WorldGen.PlaceTile(i, j - 1, ModContent.TileType<Seagrass>(), true, true, -1, Main.rand.Next(16));

			for (int k = i - 1; k < i + 2; ++k)
			{
				for (int l = j - 1; l < j + 2; ++l)
				{
					if (k == i && l == j)
						continue; //Dont check myself

					Tile cur = Framing.GetTileSafely(k, l);
					if (!cur.HasTile && woods.Contains(cur.TileType) && cur.LiquidAmount > 155 && cur.LiquidType == LiquidID.Water && Main.rand.NextBool(6))
						WorldGen.PlaceTile(k, l, ModContent.TileType<Mussel>());
				}
			}
		}
	}
}
