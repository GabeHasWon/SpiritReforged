namespace SpiritReforged.Common;

public static class SpiritSets
{
	internal static SetFactory TileFactory = new(TileLoader.TileCount, nameof(SpiritSets));

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileFactory.CreateIntSet();
}