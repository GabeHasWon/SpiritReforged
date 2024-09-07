using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaGrass : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMerge[Type][Type] = true;
		Main.tileBlockLight[Type] = true;

		Main.tileMerge[ModContent.TileType<SavannaDirt>()][Type] = true;
		Main.tileMerge[Type][ModContent.TileType<SavannaDirt>()] = true;

		TileID.Sets.Grass[Type] = true;
		TileID.Sets.NeedsGrassFramingDirt[Type] = ModContent.TileType<SavannaDirt>();
		TileID.Sets.Conversion.Grass[Type] = true;
		TileID.Sets.CanBeDugByShovel[Type] = true;

		AddMapEntry(new Color(104, 156, 70));
	}

	public override bool CanExplode(int i, int j)
	{
		WorldGen.KillTile(i, j, false, false, true); //Makes the tile completely go away instead of reverting to dirt
		return true;
	}

	public override void RandomUpdate(int i, int j)
	{
		//Tile tile = Framing.GetTileSafely(i, j);
		//Tile tileBelow = Framing.GetTileSafely(i, j + 1);
		//Tile tileAbove = Framing.GetTileSafely(i, j - 1);

		//Try place vine
		//if (WorldGen.genRand.NextBool(15) && !tileBelow.HasTile && !(tileBelow.LiquidType == LiquidID.Lava))
		//	if (!tile.BottomSlope)
		//	{
		//		tileBelow.TileType = (ushort)ModContent.TileType<BriarVines>();
		//		tileBelow.HasTile = true;
		//		WorldGen.SquareTileFrame(i, j + 1, true);
		//		if (Main.netMode == NetmodeID.Server)
		//			NetMessage.SendTileSquare(-1, i, j + 1, 3, TileChangeType.None);
		//	}

		////try place foliage
		//if (WorldGen.genRand.NextBool(25) && !tileAbove.HasTile && !(tileBelow.LiquidType == LiquidID.Lava))
		//	if (!tile.BottomSlope && !tile.TopSlope && !tile.IsHalfBlock && !tile.TopSlope)
		//	{
		//		tileAbove.TileType = (ushort)ModContent.TileType<BriarFoliage>();
		//		tileAbove.HasTile = true;
		//		tileAbove.TileFrameY = 0;
		//		tileAbove.TileFrameX = (short)(WorldGen.genRand.Next(8) * 18);
		//		WorldGen.SquareTileFrame(i, j + 1, true);
		//		if (Main.netMode == NetmodeID.Server)
		//			NetMessage.SendTileSquare(-1, i, j - 1, 3, TileChangeType.None);
		//	}

		//Try spread grass
		if (SpreadHelper.Spread(i, j, Type, 4, ModContent.TileType<SavannaDirt>()) && Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 3, TileChangeType.None);
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail) //Change self into dirt
		{
			fail = true;
			Framing.GetTileSafely(i, j).TileType = TileID.Dirt;
		}
	}
}
