namespace SpiritReforged.Common;

public static class SpiritSets
{
	internal static SetFactory ItemFactory = new(ItemLoader.ItemCount, "SpiritItems");
	internal static SetFactory TileFactory = new(TileLoader.TileCount, "SpiritTiles");
	internal static SetFactory WallFactory = new(WallLoader.WallCount, "SpiritWalls");

	/// <summary> Whether this type should grant the "Timber" achievement. </summary>
	public static readonly bool[] Timber = ItemFactory.CreateBoolSet();

	/// <summary> Whether this type is a workbench and should grant the "Benched" achievement when crafted.<para/>
	/// <see cref="ItemID.Sets.Workbenches"/> is the vanilla counterpart. </summary>
	public static readonly bool[] Workbench = ItemFactory.CreateBoolSet();

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileFactory.CreateIntSet();

	/// <summary> Whether this type is a dungeon wall variant. </summary>
	public static readonly bool[] DungeonWall = WallFactory.CreateBoolSet(WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonTileUnsafe, WallID.BlueDungeonUnsafe, WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe, WallID.GreenDungeonUnsafe, WallID.PinkDungeonSlabUnsafe, WallID.PinkDungeonTileUnsafe, WallID.PinkDungeonUnsafe);
}