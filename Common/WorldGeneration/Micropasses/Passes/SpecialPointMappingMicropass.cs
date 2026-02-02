using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using SpiritReforged.Common.WorldGeneration.PointOfInterest;
using SpiritReforged.Content.Desert.DragonFossil;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Forest.Botanist.Tiles;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Ocean.Items.Pearl;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.DataStructures;
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
		HashSet<int> curiosityTypes = [ModContent.TileType<BlunderbussTile>(), ModContent.TileType<PearlStringTile>(), ModContent.TileType<SkeletonHand>(), ModContent.TileType<Scarecrow>()];
		HashSet<InterestType> placed = [];

		for (int i = 10; i < Main.maxTilesX - 20; i++)
		{
			for (int j = 10; j < Main.maxTilesY - 20; j++)
			{
				Tile tile = Main.tile[i, j];

				if (TileExtensions.TryGetChestID(i, j, out VanillaChestID chestType))
				{
					if (chestType == VanillaChestID.Sky)
						Add(i, j, InterestType.FloatingIsland);
				}
				else if (tile.HasTile)
				{
					if (tile.TileType == TileID.Larva && tile.TileFrameX == 0 && tile.TileFrameY == 0)
						Add(i, j, InterestType.Hive);
					else if (tile.TileType == ModContent.TileType<SavannaGrass>() && !placed.Contains(InterestType.Savanna))
						Add(i, j, InterestType.Savanna);
					else if (tile.TileType == TileID.LargePiles2 && tile.TileFrameX == 920 && tile.TileFrameY == 0)
						Add(i, j, InterestType.EnchantedSword);
					else if (Fables.Enabled && TryGetWulfrumVaultType(out int type) && type == tile.TileType && TileObjectData.IsTopLeft(i, j))
						Add(i, j, InterestType.WulfrumBunker);
					else if (tile.TileType == ModContent.TileType<SaltBlockReflective>() && !placed.Contains(InterestType.SaltFlat))
						Add(i, j, InterestType.SaltFlat);
					else
					{
						if (curiosityTypes.Contains(tile.TileType) && TileObjectData.IsTopLeft(i, j))
							Add(i, j, InterestType.Curiosity);

						if (tile.TileType == ModContent.TileType<AmberFossil>() && TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity entity) 
							&& entity is FossilEntity fossil && fossil.itemType == ModContent.ItemType<TinyDragon>())
							Add(i, j, InterestType.Curiosity);
					}
                }
			}
		}

		if (Thorium.Enabled && ((Mod)Thorium).Call("GetBloodChamberBounds") is Rectangle bounds)
			Add(bounds.Center.X, bounds.Center.Y, InterestType.BloodAltar);

		foreach (Microbiome biome in MicrobiomeSystem.Microbiomes)
		{
			if (biome is ButterflyShrineBiome butterflyBiome)
				Add(butterflyBiome.Position.X, butterflyBiome.Position.Y, InterestType.ButterflyShrine);
			else if (biome is ZigguratBiome zigguratBiome)
				Add(zigguratBiome.Position.X, zigguratBiome.Position.Y, InterestType.Ziggurat);
		}

		Add((int)GenVars.shimmerPosition.X, (int)GenVars.shimmerPosition.Y, InterestType.Shimmer);

		void Add(int x, int y, InterestType type)
		{
			PointOfInterestSystem.InterestByPosition.Add(new(x, y), new(type));
			placed.Add(type);
		}
	}
}