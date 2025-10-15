using System.Linq;
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

		public bool TileFits(int i, int j) => Definition.ValidIds.Contains(Main.tile[i, j].TileType);
		public bool SurroundedBy(string one, string two) => Left.Name == one && Right.Name == two || Left.Name == two && Right.Name == one;

		public override string ToString() => $"{Start} to {End}; of {Definition}:{SurfacePoints.Count}";
	}

	public const int TransitionLength = 20;
	public static bool Mapped => Entries.Count != 0;

	internal static readonly Dictionary<short, short> TotalSurfaceY = [];
	private static List<EcotoneEntry> Entries = [];

	public override void ClearWorld()
	{
		TotalSurfaceY.Clear();
		Entries.Clear();
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		if (tasks.FindIndex(x => x.Name == "Corruption") is int index && index != -1)
		{
			foreach (var ecotone in EcotoneBase.Ecotones)
				ecotone.AddTasks(tasks, Entries);
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
				EcotoneEdgeDefinition old = entry.Definition;
				entry.End = new Point(x, y);
				entry.Right = def;
				Entries.Add(entry);

				if (x <= GenVars.leftBeachEnd || x >= GenVars.rightBeachStart)
					def = EcotoneEdgeDefinitions.GetEcotone("Ocean");

				entry = new EcotoneEntry(new Point(x, y), def);
				entry.Left = old;
				transitionCount = 0;
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