using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes.Ziggurat;
using SpiritReforged.Common.WorldGeneration.PointOfInterest;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using SpiritReforged.Content.Desert.DragonFossil;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Forest.Botanist.Tiles;
using SpiritReforged.Content.Forest.Graveyard;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Ocean.Items.Pearl;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

#nullable enable

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

			Type type = asm.GetType("Spooky.Content.Generation.VegetableGarden")!;
			MethodInfo greenhouseHook = type.GetMethod("CanPlaceGreenhouse")!;
			MonoModHooks.Add(greenhouseHook, CheckAddGreenhouse);

			Type hellType = asm.GetType("Spooky.Content.Generation.SpookyHell")!;
			MethodInfo arenaHook = hellType.GetMethod("GenerateNoseTempleStructure")!;
			MonoModHooks.Add(arenaHook, CheckAddArena);

			Type rottenType = asm.GetType("Spooky.Content.Generation.ZombieOcean")!;
			MethodInfo rottenHook = rottenType.GetMethod("PlaceAmbience")!;
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
		HashSet<string> placed = [];
		IEnumerable<InterestDefinition> definitions = ModContent.GetContent<InterestDefinition>();

		for (int i = 20; i < Main.maxTilesX - 20; i++)
		{
			for (int j = 20; j < Main.maxTilesY - 20; j++)
			{
				Tile tile = Main.tile[i, j];

				if (!tile.HasTile)
					continue;

				foreach (InterestDefinition def in definitions)
				{
					if (def.MappedInterest && def.IsInterestAt(i, j, placed))
					{
						Add(i, j, InterestType.Custom, def);
						break;
					}
				}
			}
		}

		// Spooky mod content
		if (CrossMod.Spooky.CheckFind("Krampus", out ModNPC krampus) && NPC.FindFirstNPC(krampus.Type) is { } index and not -1)
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

		foreach (MicrobiomeSystem.Microbiome biome in MicrobiomeSystem.Microbiomes)
		{
			if (biome is ButterflyMicropass.ButterflyShrineBiome butterflyBiome)
				Add(butterflyBiome.Position.X, butterflyBiome.Position.Y, InterestType.ButterflyShrine);
			else if (biome is ZigguratMicropass.ZigguratBiome zigguratBiome)
				Add(zigguratBiome.Position.X, zigguratBiome.Position.Y, InterestType.Ziggurat);
		}

		Add((int)GenVars.shimmerPosition.X, (int)GenVars.shimmerPosition.Y, InterestType.Shimmer);

		void Add(int x, int y, InterestType type, InterestDefinition? definition = null)
		{
			if (PointOfInterestSystem.InterestByPosition.TryGetValue(new Point16(x, y), out var value))
				SpiritReforgedMod.Instance.Logger.Info($"Two (or more) interests were placed @ {x}, {y}, currently {value.legacyType}, attempted {type}.");

			PointOfInterestSystem.Interest interest = definition is not null ? new(definition) : new(type);
			PointOfInterestSystem.InterestByPosition.TryAdd(new(x, y), interest);
			placed.Add(interest.definition.FullName);
		}
	}
}