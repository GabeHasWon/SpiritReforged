using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Common.WorldGeneration.PointOfInterest;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Forest.Botanist.Tiles;
using SpiritReforged.Content.Forest.ButterflyStaff;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Ocean.Items.Pearl;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.IO;
using Terraria.WorldBuilding;
using static SpiritReforged.Common.ModCompat.CrossMod;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class SpecialPointMappingMicropass : Micropass
{
	public override string WorldGenName => "Points of Interest";

	//Fables compatibility
	private static int WulfrumVaultType = -1;
	private static bool TryGetWulfrumVaultType(out int type)
	{
		if (WulfrumVaultType != -1)
		{
			type = WulfrumVaultType;
			return true;
		}

		if (Fables.TryFind("WulfrumVault", out ModTile tile))
		{
			type = WulfrumVaultType = tile.Type;
			return true;
		}

		type = 0;
		return false;
	}

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = false;
		return passes.Count - 1;
	}

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		for (int i = 10; i < Main.maxTilesX - 10; ++i)
		{
			for (int j = 10; j < Main.maxTilesY - 10; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (TileExtensions.TryGetChestID(i, j, out VanillaChestID chestType))
				{
					if (chestType == VanillaChestID.Sky)
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.FloatingIsland);
				}
				else if (tile.HasTile)
				{
					if (tile.TileType == TileID.Larva && tile.TileFrameX == 0 && tile.TileFrameY == 0)
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.Hive);
					else if (tile.LiquidType == LiquidID.Shimmer && tile.LiquidAmount > 0 && !PointOfInterestSystem.HasInterestType(InterestType.Shimmer))
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.Shimmer);
					else if (tile.TileType == ModContent.TileType<SavannaGrass>() && !PointOfInterestSystem.HasInterestType(InterestType.Savanna))
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.Savanna);
					else if (tile.TileType == TileID.LargePiles2 && tile.TileFrameX == 920 && tile.TileFrameY == 0)
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.EnchantedSword);
                    else if (tile.TileType == ModContent.TileType<ButterflyStump>() && tile.TileFrameX == 0 && tile.TileFrameY == 0)
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.ButterflyShrine);
					else if (Fables.Enabled && TryGetWulfrumVaultType(out int type) && type == tile.TileType && TileObjectData.IsTopLeft(i, j))
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.WulfrumBunker);
					else if (tile.TileType == ModContent.TileType<SaltBlockReflective>() && !PointOfInterestSystem.HasInterestType(InterestType.SaltFlat))
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.SaltFlat);
					else if (tile.TileType == ModContent.TileType<RedSandstoneBrick>() && !PointOfInterestSystem.HasInterestType(InterestType.Ziggurat))
						PointOfInterestSystem.AddPoint(new(i, j), InterestType.Ziggurat);
					else
					{
						HashSet<int> curiosityTypes = [ModContent.TileType<BlunderbussTile>(), ModContent.TileType<PearlStringTile>(), 
							ModContent.TileType<SkeletonHand>(), ModContent.TileType<Scarecrow>()];

						if (curiosityTypes.Contains(tile.TileType) && TileObjectData.IsTopLeft(i, j))
							PointOfInterestSystem.AddPoint(new(i, j), InterestType.Curiosity);
					}
                }
			}
		}

		if (Thorium.Enabled && ((Mod)Thorium).Call("GetBloodChamberBounds") is Rectangle bounds)
			PointOfInterestSystem.AddPoint(bounds.Center().ToPoint16(), InterestType.BloodAltar);

		PointOfInterestSystem.Instance.WorldGen_PointsOfInterestByPosition = PointOfInterestSystem.Instance.PointsOfInterestByPosition;
		PointOfInterestSystem.Instance.WorldGen_TakenInterestTypes = PointOfInterestSystem.Instance.TakenInterestTypes;
	}
}