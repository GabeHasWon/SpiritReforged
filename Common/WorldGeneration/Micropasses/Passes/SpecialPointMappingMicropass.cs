using SpiritReforged.Common.ModCompat;
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
using System.Reflection;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class SpecialPointMappingMicropass : Micropass
{
	public override string WorldGenName => "Points of Interest";

	private readonly static List<Point> GreenhousePositions = [];

	private static Point NoseArena = new(-1, -1);
	private static Point RottenDepths = new(-1, -1);

	public override void PostSetupContent(Mod mod)
	{
		if (CrossMod.Spooky.Enabled)
		{
			Assembly asm = CrossMod.Spooky.Instance.Code;

			Type type = asm.GetType("Spooky.Content.Generation.VegetableGarden");
			MethodInfo greenhouseHook = type.GetMethod("CanPlaceGreenhouse");
			MonoModHooks.Add(greenhouseHook, CheckAddGreenhouse);

			Type hellType = asm.GetType("Spooky.Content.Generation.SpookyHell");
			MethodInfo arenaHook = hellType.GetMethod("GenerateNoseTempleStructure");
			MonoModHooks.Add(arenaHook, CheckAddArena);

			Type rottenType = asm.GetType("Spooky.Content.Generation.ZombieOcean");
			MethodInfo rottenHook = rottenType.GetMethod("PlaceAmbience");
			MonoModHooks.Add(rottenHook, CheckRottenDepths);
		}
	}

	private static void CheckRottenDepths(Action<object, int, int, int, int> orig, object self, int PositionX, int PositionY, int SizeX, int SizeY)
	{
		orig(self, PositionX, PositionY, SizeX, SizeY);
		RottenDepths = new Point(PositionX + SizeX / 2, PositionY + SizeY / 2);
	}

	private static void CheckAddArena(Action<object, int, int, string, int, int> orig, object self, int startX, int startY, string StructureFile, int offsetX, int offsetY)
	{
		orig(self, startX, startY, StructureFile, offsetX, offsetY);

		if (StructureFile == "MinibossArena")
			NoseArena = new Point(startX + offsetX / 2, startY + offsetY / 2);
	}

	public static bool CheckAddGreenhouse(Func<object, int, int, bool> orig, object self, int PositionX, int PositionY)
	{
		bool canPlace = orig(self, PositionX, PositionY);

		if (canPlace)
			GreenhousePositions.Add(new Point(PositionX, PositionY));

		return canPlace;
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

		int fablesVaultType = -1;

		if (CrossMod.Fables.CheckFind("WulfrumVault", out ModTile fablesVault))
			fablesVaultType = fablesVault.Type;

		for (int i = 20; i < Main.maxTilesX - 20; i++)
		{
			for (int j = 20; j < Main.maxTilesY - 20; j++)
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
					else if (fablesVaultType == tile.TileType && TileObjectData.IsTopLeft(i, j))
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

		// Spooky mod content
		if (CrossMod.Spooky.TryFind("Krampus", out ModNPC krampus) && NPC.FindFirstNPC(krampus.Type) is { } index and not -1)
		{
			Point pos = Main.npc[index].Center.ToTileCoordinates();
			Add(pos.X, pos.Y, InterestType.Spooky_KrampusWorkshop);
		}

		foreach (Point greenhouse in GreenhousePositions)
			Add(greenhouse.X, greenhouse.Y, InterestType.Spooky_FetidFarms);

		if (NoseArena != new Point(-1, -1))
			Add(NoseArena.X, NoseArena.Y, InterestType.Spooky_NoseCult);

		if (RottenDepths != new Point(-1, -1))
			Add(RottenDepths.X, RottenDepths.Y, InterestType.Spooky_RottenDepths);

		// Thorium
		if (CrossMod.Thorium.TryCall(out Rectangle bounds, "GetBloodChamberBounds"))
			Add(bounds.Center.X, bounds.Center.Y, InterestType.BloodAltar);

		foreach (Microbiome biome in MicrobiomeSystem.Microbiomes)
		{
			if (biome is ButterflyShrineBiome butterflyBiome)
				Add(butterflyBiome.Position.X, butterflyBiome.Position.Y, InterestType.ButterflyShrine);
			else if (biome is ZigguratMicrobiome zigguratBiome)
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