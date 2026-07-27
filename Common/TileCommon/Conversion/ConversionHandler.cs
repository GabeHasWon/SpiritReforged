using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Common.TileCommon.Conversion;

/// <summary> Automatically defines a set in <see cref="ConversionHandler.SetByName"/>.<br/>
/// Remember that this tends to create additional uneeded sets for derived types. </summary>
public interface ISetConversion
{
	public ConversionHandler.Set ConversionSet { get; }
}

/// <summary> Controls additional behaviour for <see cref="GlobalTile.TileFrame"/> using <see cref="FrameAction"/>.<br/>
/// A common use case is for conversion based on strict tile conditions, like plants, which convert from anchors.<para/>
/// Delegates can be registered manually to any type using <see cref="AddFrameAction"/>. </summary>
public class ConversionHandler : GlobalTile
{
	public sealed class Set : Dictionary<int, int>; //Make a wrapper class to futureproof

	public delegate bool FrameDelegate(int i, int j, int type);

	public const string Vines = "Vine";
	public const string Plants = "Plants";

	private static readonly Dictionary<int, FrameDelegate> DelegatesByType = [];
	private static readonly Dictionary<string, Set> SetByName = [];

	public override void SetStaticDefaults()
	{
		foreach (var modTile in ModContent.GetContent<ModTile>())
		{
			if (modTile is ISetConversion i && i.ConversionSet is Set s)
				CreateSet(modTile.Name, s);
		}

		Set plantsSet = new()
		{
			{ TileID.CorruptGrass, TileID.CorruptPlants },
			{ TileID.CrimsonGrass, TileID.CrimsonPlants },
			{ TileID.HallowedGrass, TileID.HallowedPlants },
			{ TileID.Grass, TileID.Plants },
			{ ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaFoliageCorrupt>() },
			{ ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaFoliageCrimson>() },
			{ ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaFoliageHallow>() },
			{ ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaFoliage>() },
			{ ModContent.TileType<StargrassTile>(), ModContent.TileType<StargrassFlowers>() },
		};

		// This list handles plants that will automatically convert to other plants.
		List<int> convertablePlants = [TileID.Plants, TileID.Plants2, TileID.CorruptPlants, TileID.CrimsonPlants, TileID.HallowedPlants, TileID.HallowedPlants2];
		List<int> convertableVines = [TileID.Vines, TileID.VineFlowers, TileID.CorruptVines, TileID.CrimsonVines, TileID.HallowedVines];

		if (CrossMod.Spooky.Enabled)
		{
			plantsSet.Add(ModContent.TileType<OrangeSpookyStargrass>(), ModContent.TileType<OrangeStargrassPlants>());
			plantsSet.Add(ModContent.TileType<GreenSpookyStargrass>(), ModContent.TileType<GreenStargrassPlants>());

			if (CrossMod.Spooky.CheckFind("SpookyWeedsOrange", out ModTile orangeWeeds) && CrossMod.Spooky.CheckFind("SpookyGrass", out ModTile orange))
			{
				plantsSet.Add(orange.Type, orangeWeeds.Type);
				convertablePlants.Add(orangeWeeds.Type);
			}

			if (CrossMod.Spooky.CheckFind("SpookyWeedsGreen", out ModTile greenWeeds) && CrossMod.Spooky.CheckFind("SpookyGrassGreen", out ModTile green))
			{
				plantsSet.Add(green.Type, greenWeeds.Type);
				convertablePlants.Add(greenWeeds.Type);
			}
		}

		CreateSet(Plants, plantsSet);

		AddFrameActions(CommonPlants, [.. convertablePlants]);
		AddFrameActions(CommonVines, [.. convertableVines]);
	}

	/// <summary> Caches <paramref name="conversions"/> by <paramref name="name"/> to be easily accessed at <see cref="SetByName"/>.<br/>
	/// If the set already exists, adds the contents of <paramref name="set"/> to the existing set. </summary>
	public static void CreateSet(string name, Set set)
	{
		if (!SetByName.TryAdd(name, set))
		{
			foreach (int key in set.Keys)
				SetByName[name].TryAdd(key, set[key]);
		}
	}

	/// <summary> Outputs the set value associated with <paramref name="name"/> and <paramref name="key"/>.<para/>
	/// For example, '<see cref="nameof(SavannaGrass)"/>, <see cref="BiomeConversionID.Corruption"/>' would output the type of <see cref="SavannaGrassCorrupt"/>. </summary>
	/// <param name="name"> The name used to identify the set. This is usually the internal name of the first tile associated with it. </param>
	/// <param name="key"> The key identifier for this set. This could either be a <see cref="BiomeConversionID"/> or a TileID depending on the nature of the conversion. </param>
	/// <param name="value"> The value resulting from both prior identifiers. </param>
	public static bool FindSet(string name, int key, out int value)
	{
		value = 0;

		if (SetByName.TryGetValue(name, out var a) && a.TryGetValue(key, out int b))
		{
			value = b;
			return true;
		}

		return false;
	}

	/// <summary> Allows binding additional actions to <paramref name="type"/> when framed. Commonly used for conversion by anchor type. </summary>
	public static void AddFrameAction(int type, FrameDelegate action) => DelegatesByType.TryAdd(type, action);

	/// <summary> Allows binding additional actions to <paramref name="types"/> when framed. Commonly used for conversion by anchor type. </summary>
	public static void AddFrameActions(FrameDelegate action, params int[] types)
	{
		foreach (int type in types)
			AddFrameAction(type, action);
	}

	public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		if (DelegatesByType.TryGetValue(type, out var action))
			action.Invoke(i, j, type);

		return true;
	}

	#region actions
	/// <summary> Allows several plants to convert interchangeably between eachother when framed.<para/>
	/// See <see cref="Conversions"/> if you need a tile type to be included. </summary>
	internal static bool CommonPlants(int i, int j, int type)
	{
		if (FindSet(Plants, Framing.GetTileSafely(i, j + 1).TileType, out int newType) && type != newType)
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
		if (TileID.Sets.IsVine[type] && FindSet(Vines, Framing.GetTileSafely(i, j - 1).TileType, out int newType))
		{
			VineTile.ConvertVines(i, j, newType);
			return true;
		}

		return false;
	}
	#endregion
}