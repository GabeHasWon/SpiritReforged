namespace SpiritReforged.Common;

public static class SpiritSets
{
	internal static SetFactory ItemFactory = new(ItemLoader.ItemCount, "SpiritItems");
	internal static SetFactory TileFactory = new(TileLoader.TileCount, "SpiritTiles");
	internal static SetFactory WallFactory = new(WallLoader.WallCount, "SpiritWalls");

	/// <summary> Whether this item is considered a sword and should be compatible with the sword stand.<para/>
	/// Added in <see cref="Content.Desert.Tiles.SwordStand.RegisterIsSword"/>. </summary>
	public static readonly bool[] IsSword = ItemFactory.CreateBoolSet();

	/// <summary> Whether this type should grant the "Timber" achievement. </summary>
	public static readonly bool[] Timber = ItemFactory.CreateBoolSet();

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileFactory.CreateIntSet();

	/// <summary> Determines the draw height of this basic tile. </summary>
	public static readonly int[] FrameHeight = TileFactory.CreateIntSet();

	/// <summary> Whether this type is a dungeon wall variant. </summary>
	public static readonly bool[] DungeonWall = WallFactory.CreateBoolSet(WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonTileUnsafe, WallID.BlueDungeonUnsafe, WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe, WallID.GreenDungeonUnsafe, WallID.PinkDungeonSlabUnsafe, WallID.PinkDungeonTileUnsafe, WallID.PinkDungeonUnsafe);
}