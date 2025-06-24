using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Common.TileCommon.Conversion;

/// <summary> Controls additional behaviour for <see cref="GlobalTile.TileFrame"/> using <see cref="FrameAction"/>.<br/>
/// A common use case is for conversion based on strict tile conditions, like plants, which convert from anchors.<para/>
/// Delegates can be registered manually to any type using <see cref="AddFrameAction"/>. </summary>
public class ConvertAdjacent : GlobalTile
{
	public delegate bool FrameDelegate(int i, int j, int type);
	private static readonly Dictionary<int, HashSet<FrameDelegate>> DelegatesByType = [];

	public override void SetStaticDefaults()
	{
		AddFrameActions(CommonPlants, TileID.Plants, TileID.Plants2, TileID.CorruptPlants, TileID.CrimsonPlants, TileID.HallowedPlants, TileID.HallowedPlants2);
		AddFrameActions(CommonVines, TileID.Vines, TileID.VineFlowers, TileID.CorruptVines, TileID.CrimsonVines, TileID.HallowedVines);
	}

	/// <summary> Allows binding additional actions to <paramref name="type"/> when framed. Commonly used for conversion by anchor type. </summary>
	public static void AddFrameAction(int type, FrameDelegate action)
	{
		if (!DelegatesByType.TryAdd(type, [action]))
			DelegatesByType[type].Add(action);
	}

	/// <summary> Allows binding additional actions to <paramref name="types"/> when framed. Commonly used for conversion by anchor type. </summary>
	public static void AddFrameActions(FrameDelegate action, params int[] types)
	{
		foreach (int type in types)
			AddFrameAction(type, action);
	}

	public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		if (DelegatesByType.TryGetValue(type, out var actions))
			InvokeAll(actions, i, j, type);

		return true;
	}

	private static void InvokeAll(IEnumerable<FrameDelegate> actions, int i, int j, int type)
	{
		foreach (var action in actions)
		{
			if (action.Invoke(i, j, type))
				return;
		}
	}

	#region actions
	/// <summary> Contains anchor-to-type conversions for <see cref="CommonPlants"/>.<br/>
	/// Do <b>NOT</b> read this dict before <see cref="ModType.SetStaticDefaults"/>. </summary>
	public static readonly Dictionary<int, int> Conversions = new()
	{
		{ TileID.CorruptGrass, TileID.CorruptPlants },
		{ TileID.CrimsonGrass, TileID.CrimsonPlants },
		{ TileID.HallowedGrass, TileID.HallowedPlants },
		{ TileID.Grass, TileID.Plants },
		{ ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaFoliageCorrupt>() },
		{ ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaFoliageCrimson>() },
		{ ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaFoliageHallow>() },
		{ ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaFoliage>() },
		{ ModContent.TileType<StargrassTile>(), ModContent.TileType<StargrassFlowers>() }
	};

	/// <summary> Handles automatic one-to-one conversion in <see cref="TileFrame"/>, like vanilla vines do. </summary>
	public static readonly Dictionary<int, int> TileToVine = [];

	/// <summary> Allows several plants to convert interchangeably between eachother when framed.<para/>
	/// See <see cref="Conversions"/> if you need a tile type to be included. </summary>
	internal static bool CommonPlants(int i, int j, int type)
	{
		if (Conversions.TryGetValue(Framing.GetTileSafely(i, j + 1).TileType, out int newType) && type != newType)
		{
			if (newType == TileID.HallowedPlants && type == TileID.HallowedPlants2 || newType == TileID.Plants && type == TileID.Plants2)
				return false; //Specifically prevents `Plants2` from converting into `Plants` spontaneously (a discrepancy due to the above registry in SetStaticDefaults)

			Main.tile[i, j].TileType = (ushort)newType;
			WorldGen.Reframe(i, j, true);

			return true;
		}

		return false;
	}

	/// <summary> Allows several vines to convert interchangeably between eachother when framed. </summary>
	internal static bool CommonVines(int i, int j, int type)
	{
		if (TileID.Sets.IsVine[type] && TileToVine.TryGetValue(Framing.GetTileSafely(i, j - 1).TileType, out int value))
		{
			VineTile.ConvertVines(i, j, value);
			return true;
		}

		return false;
	}
	#endregion

	public static bool RegisterFrameFunction(object[] args) //Used for mod call
	{
		if (args.Length != 2)
			throw new ArgumentException("args must be 2 elements long (int/int[] tileType(s), Func<int, int, int, bool> function)!");

		int[] tileTypes = [];
		if (args[0] is int or short or ushort)
		{
			tileTypes = [(int)args[0]];
		}
		else if (args[0] is int[] or short[] or ushort[])
		{
			tileTypes = (int[])args[0];
		}

		if (args[1] is Func<int, int, int, bool> function)
		{
			foreach (int tileType in tileTypes)
				AddFrameAction(tileType, new FrameDelegate(function));
		}
		else
		{
			throw new ArgumentException("RegisterFrameFunction must be a Func<int, int, int, bool>!");
		}

		return true;
	}
}