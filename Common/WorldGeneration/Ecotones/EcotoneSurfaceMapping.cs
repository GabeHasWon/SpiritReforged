using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Ecotones;

internal class EcotoneSurfaceMapping : ModSystem
{
	public class EcotoneEntry(Point start, EcotoneEdgeDefinition definition)
	{
		public Point Start = start;
		public Point End;
		public HashSet<Point> SurfacePoints = [];
		public EcotoneEdgeDefinition Definition = definition;
		public EcotoneEdgeDefinition Left;
		public EcotoneEdgeDefinition Right;
		public int CorruptionType = BiomeConversionID.Purity;

		public bool TileFits(int i, int j) => Definition.ValidIds.Contains(Main.tile[i, j].TileType);
		public bool SurroundedBy(string one, string two) => Left.Name == one && Right.Name == two || Left.Name == two && Right.Name == one;

		public override string ToString() => $"{Start} to {End}; of {Definition}:{SurfacePoints.Count}";
	}

	public const int TransitionLength = 20;

	private static ILHook _modifyCorruptionHook = null;

	internal static readonly HashSet<Point> TotalSurfacePoints = [];
	internal static readonly Dictionary<short, short> TotalSurfaceY = [];
	internal static readonly Dictionary<int, Dictionary<Point16, float>> CorruptAreas = [];

	/// <summary>
	/// For some reason, the Corruption pass *really* spams "area replacement" code. So this just accounts for that.
	/// </summary>
	internal static readonly HashSet<int> SkipCorruptAreaScanXs = [];

	/// <summary>
	/// Stores the "crimson entrances" generated for future use, if desired.
	/// </summary>
	internal static readonly HashSet<Point16> CrimsonOpenings = [];

	private List<EcotoneEntry> Entries = [];

	public override void SetStaticDefaults()
	{
		var passes = GetVanillaGenPasses(null);
		var corruptionPass = passes.First(x => x.Value.Name == "Corruption").Value;

		if (corruptionPass is PassLegacy pass)
			_modifyCorruptionHook = new ILHook(GetUnderlyingMethod(pass).GetMethodInfo(), GetCorruptionAreaInfo);

		On_WorldGen.CrimStart += LogCrimsonOpening;
	}

	private void LogCrimsonOpening(On_WorldGen.orig_CrimStart orig, int i, int j)
	{
		orig(i, j);

		CrimsonOpenings.Add(new Point16(i, j));
	}

	private void GetCorruptionAreaInfo(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(MoveType.After, x => x.MatchCall<WorldGen>(nameof(WorldGen.CrimStart))))
			return;

		if (!c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<Main>(nameof(Main.worldSurface))))
			return;

		if (!c.TryGotoNext(MoveType.After, x => x.MatchStloc(22)))
			return;

		c.Emit(OpCodes.Ldloc_S, (byte)20); // left x
		c.Emit(OpCodes.Ldloc_S, (byte)21); // right x
		c.Emit(OpCodes.Ldloc_S, (byte)22); // bottom y
		c.Emit(OpCodes.Ldc_I4_1); // false, for crimson
		c.EmitDelegate(AddCorruptArea);

		if (!c.TryGotoNext(MoveType.After, x => x.MatchCall<WorldGen>(nameof(WorldGen.ChasmRunner))))
			return;

		for (int i = 0; i < 2; ++i)
			if (!c.TryGotoNext(x => x.MatchLdsfld<Main>(nameof(Main.worldSurface))))
				return;

		c.Emit(OpCodes.Ldloc_S, (byte)48); // left x
		c.Emit(OpCodes.Ldloc_S, (byte)49); // right x
		c.Emit(OpCodes.Ldloc_S, (byte)51); // bottom y
		c.Emit(OpCodes.Ldc_I4_0); // true, for corruption
		c.EmitDelegate(AddCorruptArea);
	}

	public static void AddCorruptArea(int leftX, int rightX, float bottomY, bool crimson)
	{
		if (SkipCorruptAreaScanXs.Contains(leftX) || SkipCorruptAreaScanXs.Contains(rightX))
			return;

		SkipCorruptAreaScanXs.Add(leftX);
		SkipCorruptAreaScanXs.Add(rightX);

		int topY = (int)GenVars.worldSurfaceLow;
		int convId = crimson ? BiomeConversionID.Crimson : BiomeConversionID.Corruption;

		CorruptAreas.TryAdd(convId, []);

		for (int i = leftX; i < rightX; ++i)
		{
			for (int j = topY; j < bottomY; ++j)
			{
				Point16 pos = new(i, j);

				if (!CorruptAreas[convId].ContainsKey(pos))
				{
					float chance = 1f;

					chance = Math.Min(chance, Utils.GetLerpValue(leftX, leftX + 30, i, true));
					chance = Math.Min(chance, Utils.GetLerpValue(rightX, rightX - 30, i, true));
					chance = Math.Min(chance, Utils.GetLerpValue(topY, topY + 30, j, true));
					chance = Math.Min(chance, Utils.GetLerpValue(bottomY, bottomY - 30, j, true));

					if (chance != 0)
						CorruptAreas[convId].Add(pos, chance);
				}
			}
		}
	}

	public override void Unload()
	{
		_modifyCorruptionHook.Dispose();
		_modifyCorruptionHook = null;
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_method")]
	private extern static ref WorldGenLegacyMethod GetUnderlyingMethod(PassLegacy pass);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_vanillaGenPasses")]
	private extern static ref Dictionary<string, GenPass> GetVanillaGenPasses(WorldGen gen);

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		int mapIndex = tasks.FindIndex(x => x.Name == "Corruption");

		if (mapIndex == -1)
			return;

		foreach (var ecotone in EcotoneBase.Ecotones)
			ecotone.AddTasks(tasks, Entries);

		tasks.Insert(mapIndex - 2, new PassLegacy("Reset Corruption Mapping", ResetCorruptionMapping));
		tasks.Insert(mapIndex + 1, new PassLegacy("Map Ecotones", MapEcotones));
		tasks.Insert(tasks.Count - 2, new PassLegacy("Re-Corrupt Areas", ReCorruptAreas));

//#if DEBUG
//		tasks.Add(new PassLegacy("Ecotone Debug", (progress, config) =>
//		{
//			foreach (var item in Entries)
//			{
//				for (int x = item.Start.X; x < item.End.X; ++x)
//				{
//					for (int nY = 90; nY < 100; ++nY)
//						WorldGen.PlaceTile(x, nY, item.Definition.DisplayId, true, true);
//				}
//			}
//		}));
//#endif
	}

	private void ResetCorruptionMapping(GenerationProgress progress, GameConfiguration configuration)
	{
		CorruptAreas.Clear();
		SkipCorruptAreaScanXs.Clear();
		CrimsonOpenings.Clear();
	}

	private void ReCorruptAreas(GenerationProgress progress, GameConfiguration configuration)
	{
		foreach (int key in CorruptAreas.Keys)
			foreach ((Point16 point, float chance) in CorruptAreas[key])
				if (WorldGen.genRand.NextFloat() < chance)
					WorldGen.Convert(point.X, point.Y, key, 0);
	}

	private void MapEcotones(GenerationProgress progress, GameConfiguration configuration)
	{
		const int StartX = 250;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Ecotones");

		Entries.Clear();
		TotalSurfacePoints.Clear();
		TotalSurfaceY.Clear();

		int transitionCount = 0;
		EcotoneEntry entry = null;
		int conversionType = BiomeConversionID.Purity;

		for (int x = StartX; x < Main.maxTilesX - StartX; ++x)
		{
			int y = 80;

			while (!WorldGen.SolidOrSlopedTile(x, y) || WorldMethods.CloudsBelow(x, y, out int addY))
				y++;

			if (entry is null)
			{
				entry = new EcotoneEntry(new Point(StartX, y), EcotoneEdgeDefinitions.GetEcotone("Ocean"));
				entry.Left = EcotoneEdgeDefinitions.GetEcotone("Ocean");
			}

			if (!entry.TileFits(x, y))
				transitionCount++;

			if (transitionCount > TransitionLength && EcotoneEdgeDefinitions.TryGetEcotoneByTile(Main.tile[x, y].TileType, out var def) && def.Name != entry.Definition.Name)
			{
				if (def.Name == "Corruption")
					conversionType = BiomeConversionID.Corruption;
				else if (def.Name == "Crimson")
					conversionType = BiomeConversionID.Crimson;
				else if (def.Name == "Hallow")
					conversionType = BiomeConversionID.Hallow;
				else
				{
					EcotoneEdgeDefinition old = entry.Definition;
					entry.End = new Point(x, y);
					entry.Right = def;
					Entries.Add(entry);

					if (x <= GenVars.leftBeachEnd || x >= GenVars.rightBeachStart)
						def = EcotoneEdgeDefinitions.GetEcotone("Ocean");

					entry = new EcotoneEntry(new Point(x, y), def);
					entry.Left = old;
					entry.CorruptionType = conversionType;
					transitionCount = 0;
					conversionType = BiomeConversionID.Purity;
				}
			}

			entry.SurfacePoints.Add(new Point(x, y));
			TotalSurfacePoints.Add(new Point(x, y));
			TotalSurfaceY.Add((short)x, (short)y);

			if (x == Main.maxTilesX - StartX - 1)
				entry.End = new Point(x, y);
		}

		entry.Right = EcotoneEdgeDefinitions.GetEcotone("Ocean");
		Entries.Add(entry);
		Entries = [.. Entries.OrderBy(x => x.Start.X)];
	}
}
