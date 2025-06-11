namespace SpiritReforged.Common;

public static class SpiritSets
{
	internal static SetFactory TileFactory = new(TileLoader.TileCount, nameof(SpiritSets));

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileFactory.CreateIntSet();
	/// <summary> Whether this type prefers to convert based on adjacent tiles rather than directly. <br/>
	/// See <see cref="TileCommon.Conversion.ConvertAdjacentSet"/> for the handler.</summary>
	public static readonly bool[] ConvertsByAdjacent = TileFactory.CreateBoolSet();
}