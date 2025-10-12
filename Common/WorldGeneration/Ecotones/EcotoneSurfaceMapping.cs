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

public class EcotoneSurfaceMapping : ModSystem
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
	public static bool Mapped => Entries.Count != 0;

	public static readonly HashSet<Point> TotalSurfacePoints = [];
	public static readonly Dictionary<short, short> TotalSurfaceY = [];
	public static readonly Dictionary<int, Dictionary<Point16, float>> CorruptAreas = [];

	public static List<EcotoneEntry> Entries { get; internal set; }

	private static ILHook _modifyCorruptionHook = null;

	/// <summary>
	/// For some reason, the Corruption pass *really* spams "area replacement" code. So this just accounts for that.
	/// </summary>
	internal static readonly HashSet<int> SkipCorruptAreaScanXs = [];

	/// <summary>
	/// Stores the "crimson entrances" generated for future use, if desired.
	/// </summary>
	internal static readonly HashSet<Point16> CrimsonOpenings = [];

	public override void ClearWorld()
	{
		TotalSurfaceY.Clear();
		Entries.Clear();
	}

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
		if (tasks.FindIndex(x => x.Name == "Corruption") is int index && index != -1)
		{
			foreach (var ecotone in EcotoneBase.Ecotones)
				ecotone.AddTasks(tasks, Entries);

			tasks.Insert(index - 2, new PassLegacy("Reset Corruption Mapping", ResetCorruptionMapping));
			tasks.Insert(tasks.Count - 2, new PassLegacy("Re-Corrupt Areas", ReCorruptAreas));
		}
	}

	/// <summary> Clears cached coordinates within the provided horizontal range and adjusts the range based on cleared ecotones. </summary>
	private static void ClearRange(ref int start, ref int end)
	{
		if (start <= 0 && end >= Main.maxTilesX) //Avoid extra computations
		{
			Entries.Clear();
			TotalSurfaceY.Clear();

			return;
		}

		int _start = start;
		int _end = end;

		var entryRemovals = Entries.Where(x => x.SurfacePoints.Any(z => z.X >= _start && z.X <= _end)).ToHashSet();

		foreach (var item in entryRemovals)
		{
			if (item.Start.X < start)
				_start = start = item.Start.X;

			if (item.End.X > end)
				_end = end = item.End.X;

			Entries.Remove(item);
		}

		var dictRemovals = TotalSurfaceY.Keys.Where(x => x >= _start && x <= _end).ToHashSet();

		foreach (short item in dictRemovals)
			TotalSurfaceY.Remove(item);
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

	/// <summary> Maps ecotones spanning the entire world. Mapping should normally be done before finding an ecotone spawn location. </summary>
	public static void MapEcotones() => MapEcotones(0, Main.maxTilesX);

	/// <summary> Maps ecotones within the provided bounds. Mapping should normally be done before finding an ecotone spawn location. </summary>
	public static void MapEcotones(int start, int end)
	{
		const int Fluff = 250;

		ClearRange(ref start, ref end);

		start = Math.Max(start, Fluff);
		end = Math.Min(end, Main.maxTilesX - Fluff);

		int transitionCount = 0;
		EcotoneEntry entry = null;
		int conversionType = BiomeConversionID.Purity;

		for (int x = start; x < end; ++x)
		{
			int y = 80;

			while (!WorldGen.SolidOrSlopedTile(x, y) || WorldMethods.CloudsBelow(x, y, out _))
				y++; //Skip over clouds

			if (entry is null)
			{
				entry = new EcotoneEntry(new Point(Fluff, y), EcotoneEdgeDefinitions.GetEcotone("Ocean"));
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

			MapPoint(x, y, entry);

			if (x == Main.maxTilesX - Fluff - 1)
				entry.End = new Point(x, y);
		}

		entry.Right = EcotoneEdgeDefinitions.GetEcotone("Ocean");
		Entries.Add(entry);
		Entries = [.. Entries.OrderBy(x => x.Start.X)];

		static void MapPoint(int x, int y, EcotoneEntry entry)
		{
			entry.SurfacePoints.Add(new Point(x, y));
			TotalSurfaceY.Add((short)x, (short)y);
		}
	}

	/// <summary> Selects the largest possible ecotone from a selection matching <paramref name="predicate"/>.<para/>
	/// Automatically remaps ecotones. </summary>
	public static EcotoneEntry FindWhere(Func<EcotoneEntry, bool> predicate)
	{
		MapEcotones();

		if (Entries.Where(predicate) is IEnumerable<EcotoneEntry> validEntries && validEntries.Any())
		{
			if (validEntries.OrderBy(x => Math.Abs(x.Start.X - x.End.X)).Last() is EcotoneEntry entry)
				return entry;
		}

		return null;
	}

	/// <summary> Returns whether both the start and end Y coordinates of <paramref name="x"/> are on the surface. </summary>
	public static bool OnSurface(EcotoneEntry x) => x.Start.Y < Main.worldSurface && x.End.Y < Main.worldSurface;

	/// <summary> Returns whether <paramref name="x"/> is overlapping the center of the world. </summary>
	public static bool OverSpawn(EcotoneEntry x)
	{
		int spawn = Main.maxTilesX / 2;
		return x.Start.X < spawn && x.End.X > spawn;
	}
}