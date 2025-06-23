using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Common.TileCommon.Conversion;

public class ConvertAdjacent : GlobalTile
{
	public delegate bool FrameDelegate(int i, int j);
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

	public static void AddFrameActions(FrameDelegate action, params int[] types)
	{
		foreach (int type in types)
			AddFrameAction(type, action);
	}

	public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		if (DelegatesByType.TryGetValue(type, out var actions))
			InvokeAll(actions, i, j);

		return true;
	}

	private static void InvokeAll(IEnumerable<FrameDelegate> actions, int i, int j)
	{
		foreach (var action in actions)
		{
			if (action.Invoke(i, j))
				return;
		}
	}

	#region actions
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

	/// <summary> Allows several plants to convert interchangeably between eachother when framed. </summary>
	internal static bool CommonPlants(int i, int j)
	{
		int type = Main.tile[i, j].TileType;

		if (Conversions.TryGetValue(Framing.GetTileSafely(i, j + 1).TileType, out int newType) && type != newType)
		{
			if (newType == TileID.HallowedPlants && type == TileID.HallowedPlants2 || newType == TileID.Plants && type == TileID.Plants2)
				return false; //Prevents `Plants2` from converting into `Plants` spontaneously

			Main.tile[i, j].TileType = (ushort)newType;
			WorldGen.Reframe(i, j);

			return true;
		}

		return false;
	}

	/// <summary> Allows several vines to convert interchangeably between eachother when framed. </summary>
	internal static bool CommonVines(int i, int j)
	{
		int type = Main.tile[i, j].TileType;

		if (TileID.Sets.IsVine[type] && TileToVine.TryGetValue(Framing.GetTileSafely(i, j - 1).TileType, out int value))
		{
			VineTile.ConvertVines(i, j, value);
			return true;
		}

		return false;
	}
	#endregion
}