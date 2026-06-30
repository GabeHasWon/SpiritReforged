using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;
using SpiritReforged.Content.Underground.Tiles;
using System.Collections;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.Tile_Entities;
using Terraria.ModLoader.Config;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration;

public class HouseLoader : ILoadable, IGenerationPage
{
	public readonly record struct BuilderResult(bool Success, string Blacklist = null);

	public static readonly BuilderResult Success = new(true);
	public static readonly BuilderResult Fail = new(false);

	public delegate BuilderResult BuilderDelegate(HouseBuilder houseBuilder);

	internal static FieldInfo DisplayDollItems { get; private set; }

	#region generation page
	[GenConfigurable(1, 20)]
	[Slider]
	[ReverseMinMax]
	[Denominator]
	private static int SignChance = 5;

	[GenConfigurable(1, 20)]
	[Slider]
	[ReverseMinMax]
	[Denominator]
	[PriorityModifier(nameof(SignChance))]
	private static int MannequinChance = 4;

	[GenConfigurable(1, 50)]
	[Slider]
	[ReverseMinMax]
	[Denominator]
	[PriorityModifier(nameof(SignChance))]
	private static int LoomChance = 10;

	[GenConfigurable(1, 100)]
	[Slider]
	[ReverseMinMax]
	[Denominator]
	[PriorityModifier(nameof(SignChance))]
	private static int RareSignChance = 25;

	PageInfo IGenerationPage.Info => new("Caves", DrawHelpers.RequestLocal(typeof(CaveDecorMicropass), "UndergroundPage", false), DrawHelpers.RequestLocal(typeof(CaveDecorMicropass), "UndergroundPageButton", false))
	{
		Presets =
		[
			new("Inhabited",
			[
				new IndividualPreset(nameof(SignChance), 3),
				new IndividualPreset(nameof(RareSignChance), 10),
				new IndividualPreset(nameof(MannequinChance), 2),
				new IndividualPreset(nameof(LoomChance), 7),
				new IndividualPreset(nameof(CaveDecorMicropass.CartSpawnRate), 14f),
				new IndividualPreset(nameof(FishingAreaMicropass.CovesSpawnRate), 2.2f),
				new IndividualPreset(nameof(PotteryStructureMicropass.StructuresMax), 3.2f),
			]),

			new("Empty",
			[
				new IndividualPreset(nameof(SignChance), 20),
				new IndividualPreset(nameof(RareSignChance), 20),
				new IndividualPreset(nameof(MannequinChance), 15),
				new IndividualPreset(nameof(LoomChance), 45),
				new IndividualPreset(nameof(CaveDecorMicropass.CartSpawnRate), 0.5f),
				new IndividualPreset(nameof(FishingAreaMicropass.CovesSpawnRate), 0.25f),
				new IndividualPreset(nameof(PotteryStructureMicropass.StructuresMax), 0.25f),
			]),
		]
	};

	Mod IGenerationPage.Mod => SpiritReforgedMod.Instance;
	#endregion

	public static event BuilderDelegate BuilderAction;

	public void Load(Mod mod)
	{
		DisplayDollItems = typeof(TEDisplayDoll).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);

		BuilderAction += FillMannequin;
		BuilderAction += FillLoom;
		BuilderAction += FillSign;

		On_HouseBuilder.Place += PostBuildHouse;
	}

	public void Unload() { }

	private static void PostBuildHouse(On_HouseBuilder.orig_Place orig, HouseBuilder self, HouseBuilderContext context, StructureMap structures)
	{
		orig(self, context, structures);

		//BuilderAction?.Invoke(self);
		if (BuilderAction != null)
		{
			HashSet<string> blacklist = [];
			IEnumerator enumerator = BuilderAction.GetInvocationList().GetEnumerator();

			while (enumerator.MoveNext())
			{
				if (enumerator.Current is BuilderDelegate dele && !blacklist.Contains(dele.Method.Name) && dele.Invoke(self).Success && dele.Invoke(self).Blacklist is string ignoreItem)
					blacklist.Add(ignoreItem);
			}
		}
	}

	#region content
	public static BuilderResult FillMannequin(HouseBuilder houseBuilder)
	{
		if (houseBuilder.Type is not HouseType.Wood and not HouseType.Desert and not HouseType.Ice)
			return Fail;

		foreach (Rectangle room in houseBuilder.Rooms)
		{
			if (!WorldGen.genRand.NextBool(MannequinChance))
				continue;

			if (TryPlace(room, TileID.DisplayDoll, out PlaceAttempt placeAttempt))
			{
				Point16 location = placeAttempt.Coords;

				ManualUpdateFrame(new(location.X, location.Y - 2, 2, 3), WorldGen.genRand.Next(4));

				int whoAmI = TEDisplayDoll.Place(location.X, location.Y);
				MannequinInventory.InventoryByBiome[houseBuilder.Type].SetMannequin(whoAmI);

				return new(true, nameof(DisplayCase.FillDisplayCase));
			}
		}

		return Fail;

		static void ManualUpdateFrame(Rectangle area, int frameNumber)
		{
			for (int i = area.Left; i < area.Right; i++)
			{
				for (int j = area.Top; j < area.Bottom; j++)
					Main.tile[i, j].TileFrameX += (short)(18 * 2 * frameNumber);
			}
		}
	}

	public static BuilderResult FillLoom(HouseBuilder houseBuilder)
	{
		if (houseBuilder.Type is not HouseType.Wood)
			return Fail;

		bool placedLoom = false;

		foreach (Rectangle room in houseBuilder.Rooms)
		{
			if (!placedLoom && WorldGen.genRand.NextBool(LoomChance) && TryPlace(room, TileID.Loom, out PlaceAttempt placeAttempt))
			{
				for (int i = 0; i < room.Width / 9; i++)
					TryPlace(new(room.X, room.Y, room.Width, 1), TileID.Banners, out _, style: Main.rand.Next(4));

				placedLoom = true;
			}

			if (placedLoom && TryFindChest(room, out Chest chest) && Array.FindIndex(chest.item, static (x) => x.IsAir) is int index && index != -1) //Search for the first instance of air
			{
				ChestPoolUtils.PlaceChestItems([new ChestPoolUtils.ChestInfo(4, 9, 1f, ItemID.Silk)], chest, index);
				return Success;
			}
		}

		return Success;
	}

	public static BuilderResult FillSign(HouseBuilder houseBuilder)
	{
		if (houseBuilder.Type is not HouseType.Wood)
			return Fail;

		foreach (Rectangle room in houseBuilder.Rooms)
		{
			if (WorldGen.genRand.NextBool(SignChance) && TryPlace(new(room.X, room.Y, room.Width, 1), TileID.Signs, out PlaceAttempt placeAttempt))
			{
				(int i, int j) = (placeAttempt.Coords.X, placeAttempt.Coords.Y);

				Main.sign[Sign.ReadSign(i, j)].text = WorldGen.genRand.NextBool(RareSignChance)
					? Language.GetTextValue("Mods.SpiritReforged.Generation.Signs.Underground.Rare." + Main.rand.Next(3))
					: Language.GetTextValue("Mods.SpiritReforged.Generation.Signs.Underground.Common." + Main.rand.Next(11));
			}
		}

		return Success;
	}
	#endregion

	public static bool TryPlace(Rectangle bounds, int type, out PlaceAttempt placeAttempt, int attempts = 0, int style = -1)
	{
		if (attempts == 0)
			attempts = bounds.Width * bounds.Height / 2;

		for (int a = 0; a < attempts; a++)
		{
			(int i, int j) = (WorldGen.genRand.Next(bounds.Left, bounds.Right + 1), WorldGen.genRand.Next(bounds.Top, bounds.Bottom + 1));

			if ((placeAttempt = Placer.Check(i, j, type, style).IsClear().Place()).success)
				return true;
		}

		placeAttempt = default;
		return false;
	}

	public static bool TryFindChest(Rectangle bounds, out Chest chest)
	{
		for (int x = bounds.Left; x < bounds.Right; x++)
		{
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				if (Chest.FindChest(x, y) is int chestIndex && chestIndex != -1)
				{
					chest = Main.chest[chestIndex];
					return true;
				}
			}
		}

		chest = null;
		return false;
	}
}