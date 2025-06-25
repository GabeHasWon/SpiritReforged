using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Forest.Misc;
using SpiritReforged.Content.Savanna.Items.Gar;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;
using static SpiritReforged.Common.WorldGeneration.QuickConversion;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class FishingAreaMicropass : Micropass
{
	[WorldBound]
	public static readonly HashSet<Rectangle> Coves = [];
	private static readonly Dictionary<int, Point16[]> OffsetsBySubId = new()
	{
		{ 0, [new Point16(9, 5), new Point16(54, 19), new Point16(6, 15)] },
		{ 1, [new Point16(30, 7), new Point16(1, 12)] },
		{ 2, [new Point16(28, 3), new Point16(2, 18), new Point16(42, 20)] },
		{ 3, [new Point16(18, 3), new Point16(6, 11), new Point16(2, 17), new Point16(34, 6), new Point16(46, 22)] },
		{ 4, [new Point16(23, 3), new Point16(4, 15), new Point16(51, 2), new Point16(49, 20)] }
	};

	public override string WorldGenName => "Fishing Coves";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Gem Caves"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.FishingCoves");
		int repeats = (int)(Main.maxTilesX / 4200f * 8);

		for (int i = 0; i < repeats; ++i)
		{
			int id = WorldGen.genRand.Next(5);
			string structureName = "Assets/Structures/Coves/FishCove" + id + WorldGen.genRand.Next(["a", "b"]);

			Point16 size = StructureHelper.API.Generator.GetStructureDimensions(structureName, SpiritReforgedMod.Instance);
			Point16 pos;

			int edge = CrossMod.Remnants.Enabled ? 400 : 200;
			int rock = (int)Main.rockLayer;

			do
			{
				pos = new Point16(WorldGen.genRand.Next(edge, Main.maxTilesX - edge), WorldGen.genRand.Next(rock, (rock + Main.maxTilesY) / 2));
			} while (!Collision.SolidCollision(pos.ToWorldCoordinates(), 32, 32));

			pos -= WorldGen.genRand.Next(OffsetsBySubId[id]);

			if (!GenVars.structures.CanPlace(new Rectangle(pos.X, pos.Y, size.X, size.Y), 10) || !AvoidsMicrobiomes(pos) || !StructureTools.SpawnConvertedStructure(pos, size, structureName, out var biome, BiomeType.Desert))
			{
				i--;
				continue;
			}

			var area = new Rectangle(pos.X, pos.Y, size.X, size.Y);
			StructureTools.ClearActuators(pos.X, pos.Y, size.X, size.Y);

			Coves.Add(area);
			GenVars.structures.AddProtectedStructure(area, 6);

			FillContainers(area, biome);
		}
	}

	private static void FillContainers(Rectangle area, BiomeType biome)
	{
		foreach (var c in Main.chest)
		{
			if (c != null && area.Contains(new Point(c.x, c.y)))
			{
				foreach (var item in c.item)
					item.TurnToAir();

				int index = 0;

				foreach (var item in GetLootPool(biome))
					c.item[index++] = item;
			}
		}
	}

	private static Item[] GetLootPool(BiomeType biome)
	{
		List<Item> items = [];

		Add(ItemID.CanOfWorms, WorldGen.genRand.Next(2, 6));
		Add(ItemID.ReinforcedFishingPole, chanceDenominator: 2);

		int[] fishOptions = biome switch
		{
			BiomeType.Jungle => [ItemID.Bass, ItemID.Honeyfin, ItemID.NeonTetra, ItemID.Stinkfish, ItemID.VariegatedLardfish],
			BiomeType.Ice => [ItemID.Bass, ItemID.ArmoredCavefish, ItemID.FrostMinnow, ItemID.AtlanticCod, ItemID.GreenJellyfish],
			_ => [ItemID.Bass, ItemID.ArmoredCavefish, ItemID.SpecularFish, ItemID.BlueJellyfish, ItemID.GreenJellyfish],
		};

		int fishStack = WorldGen.genRand.Next(1, 4);
		int fishRolls = WorldGen.genRand.NextBool(3) ? 2 : 1;

		for (int i = 0; i < fishRolls; i++)
		{
			if (WorldGen.genRand.NextBool(9) && biome is not BiomeType.Desert)
			{
				Add(ItemID.GoldenCarp);
				continue;
			}

			Add(WorldGen.genRand.Next(fishOptions), WorldGen.genRand.Next(1, 4));
		}

		for (int i = 0; i < 2; i++)
			Add(WorldGen.genRand.Next([ItemID.FishingPotion, ItemID.SonarPotion, ItemID.CratePotion]), WorldGen.genRand.Next(2, 5));

		int caveItem = WorldGen.genRand.Next([ItemID.Shuriken, ItemID.WoodenArrow, ItemID.Torch, ItemID.Glowstick, ItemID.Bomb]);
		int caveStack = (caveItem is ItemID.Shuriken or ItemID.WoodenArrow) ? WorldGen.genRand.Next(25, 51) : ((caveItem is ItemID.Torch or ItemID.Glowstick) ? WorldGen.genRand.Next(15, 31) : WorldGen.genRand.Next(10, 21));

		Add(caveItem, caveStack);

		Add(WorldGen.genRand.Next([ItemID.SpelunkerPotion, ItemID.FeatherfallPotion, ItemID.NightOwlPotion, ItemID.WaterWalkingPotion, ItemID.ArcheryPotion, ItemID.GravitationPotion]), WorldGen.genRand.Next(2, 5), 3);
		Add(WorldGen.genRand.Next([ItemID.ThornsPotion, ItemID.InvisibilityPotion, ItemID.HunterPotion, ItemID.TrapsightPotion, ItemID.TeleportationPotion]), WorldGen.genRand.Next(2, 5), 3);
		Add(WorldGen.genRand.Next([ModContent.ItemType<RemedyPotion>(), ModContent.ItemType<QuenchPotion>()]), WorldGen.genRand.Next(2, 5));

		return [.. items];

		void Add(int type, int stack = 1, int chanceDenominator = 1)
		{
			if (chanceDenominator < 2 || Main.rand.NextBool(chanceDenominator))
			{
				if (items.IndexOf(items.FirstOrDefault(x => x.type == type)) is int index && index != -1)
					items[index].stack += stack; //If the item type already exists in the pool, stack it instead
				else
					items.Add(new(type, stack));
			}
		}
	}

	/// <summary> Checks whether this structure intersects a marble or granite minibiome. </summary>
	private static bool AvoidsMicrobiomes(Point16 pos)
	{
		const int countMax = 10;
		int count = 0;

		for (int i = pos.X - 40; i < pos.X + 40; ++i)
		{
			for (int j = pos.Y - 40; j < pos.Y + 40; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (tile.HasTile && tile.TileType is TileID.Granite or TileID.Marble && ++count >= countMax)
					return false;
			}
		}

		return true;
	}
}