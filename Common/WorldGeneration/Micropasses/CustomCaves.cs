using SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses;

/// <summary> Handles replacing mountain caves with our custom caves. </summary>
internal class CustomCaves : ILoadable
{
	private static readonly Dictionary<Point16, CaveEntranceType> TypeByPosition = [];
	private static bool AddingMountainCaves = false;

	public void Load(Mod mod)
	{
		On_WorldGen.Mountinater += OverrideGenMound;
		On_WorldGen.Cavinator += ModifyCavinatorForCaveType;
		On_WorldGen.CaveOpenater += ModifyOpenator;

		var pass = WorldGen.VanillaGenPasses.Values.FirstOrDefault(x => x.Name == "Mountain Caves");

		if (pass != null)
			WorldGen.DetourPass((PassLegacy)pass, AddFlagToMountainCavePass);
	}

	public void Unload() { }

	private static void AddFlagToMountainCavePass(WorldGen.orig_GenPassDetour orig, object self, GenerationProgress progress, GameConfiguration configuration)
	{
		AddingMountainCaves = true;
		orig(self, progress, configuration);
		AddingMountainCaves = false;
	}

	private static void ModifyOpenator(On_WorldGen.orig_CaveOpenater orig, int i, int j)
	{
		if (!AddingMountainCaves || GetEntrance(i, j) is CaveEntranceType.Vanilla || CaveEntrance.EntranceByType[GetEntrance(i, j)].ModifyOpening(ref i, ref j, true))
			orig(i, j);
	}

	private static void ModifyCavinatorForCaveType(On_WorldGen.orig_Cavinator orig, int i, int j, int steps)
	{
		if (!AddingMountainCaves || GetEntrance(i, j) is CaveEntranceType.Vanilla || CaveEntrance.EntranceByType[GetEntrance(i, j)].ModifyOpening(ref i, ref j, false))
			orig(i, j, steps);
	}

	private static void OverrideGenMound(On_WorldGen.orig_Mountinater orig, int i, int j)
	{
		var type = (CaveEntranceType)WorldGen.genRand.Next((int)CaveEntranceType.Count);
		TypeByPosition.Add(new(i, j), type);

		if (type == CaveEntranceType.Vanilla)
			orig(i, j);
		else
			CaveEntrance.EntranceByType[type].Generate(i, j);
	}

	private static CaveEntranceType GetEntrance(int i, int j)
	{
		TypeByPosition.TryGetValue(new Point16(i, j), out CaveEntranceType type); //If the value isn't found, type is default (vanilla)
		return type;
	}
}