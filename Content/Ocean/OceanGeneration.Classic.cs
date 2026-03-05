using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Ocean.Tiles;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Ocean;

public partial class OceanGeneration : ModSystem
{
	private static int _duelistLegacy = -1;
	private static int _ladyLuck = -1;

	private static void GenPirateChest(Rectangle area)
	{
		if (CrossMod.Classic.Enabled && CrossMod.Classic.TryFind("LadyLuck", out ModItem luck) && CrossMod.Classic.TryFind("DuelistLegacy", out ModItem duelist))
		{
			_duelistLegacy = duelist.Type;
			_ladyLuck = luck.Type;

			WorldMethods.Generate(CreatePirateChest, 1, out _, area, 200);
		}
	}

	private static bool CreatePirateChest(int i, int j)
	{
		if (!FindSurface(i, ref j) || TilesFromEdge(i) is int inner && inner < 100)
			return false;

		if (WorldMethods.AreaClear(i, j - 1, 2, 2) && GenVars.structures.CanPlace(new Rectangle(i, j - 2, 2, 2)))
		{
			for (int w = 0; w < 4; w++)
			{
				int x = i + w % 2;
				int y = j + w / 2 + 1;

				WorldGen.KillTile(x, y, false, noItem: true);
				WorldGen.PlaceTile(x, y, TileID.HardenedSand, true);
			}

			PlaceChest(i, j, ModContent.TileType<PirateChest>(),
				[
					(RightSide(i) ? _ladyLuck : _duelistLegacy, 1)
				],
				[
					(ItemID.GoldCoin, WorldGen.genRand.Next(12, 30)), (ItemID.Diamond, WorldGen.genRand.Next(12, 30)), (ItemID.GoldCrown, 1), (ItemID.GoldDust, WorldGen.genRand.Next(1, 3)),
						(ItemID.GoldChest, 1), (ItemID.GoldenChair, 1), (ItemID.GoldChandelier, 1), (ItemID.GoldenPlatform, WorldGen.genRand.Next(12, 18)), (ItemID.GoldenSink, 1), (ItemID.GoldenSofa, 1),
						(ItemID.GoldenTable, 1), (ItemID.GoldenToilet, 1), (ItemID.GoldenWorkbench, 1), (ItemID.GoldenPiano, 1), (ItemID.GoldenLantern, 1), (ItemID.GoldenLamp, 1), (ItemID.GoldenDresser, 1),
						(ItemID.GoldenDoor, 1), (ItemID.GoldenCrate, 1), (ItemID.GoldenClock, 1), (ItemID.GoldenChest, 1), (ItemID.GoldenCandle, WorldGen.genRand.Next(2, 4)), (ItemID.GoldenBookcase, 1),
						(ItemID.TitaniumBar, WorldGen.genRand.Next(3, 7)), (ItemID.PalladiumBar, WorldGen.genRand.Next(3, 7)), (ItemID.OrichalcumBar, WorldGen.genRand.Next(3, 7))
				],
				true, WorldGen.genRand, WorldGen.genRand.Next(15, 21), 1, true, 2, 2);

			return true;
		}

		return false;
	}
}