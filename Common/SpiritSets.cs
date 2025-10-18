namespace SpiritReforged.Common;

[ReinitializeDuringResizeArrays]
public static class SpiritSets
{
	internal static SetFactory ItemFactory = new(ItemLoader.ItemCount, "SpiritItems");
	internal static SetFactory NPCFactory = new(NPCLoader.NPCCount, "SpiritNPCs");
	internal static SetFactory TileFactory = new(TileLoader.TileCount, "SpiritTiles");
	internal static SetFactory WallFactory = new(WallLoader.WallCount, "SpiritWalls");

	/// <summary> Whether this item is considered a sword and should be compatible with the sword stand.<para/>
	/// Added in <see cref="Content.Desert.Tiles.SwordStand.RegisterIsSword"/>. </summary>
	public static readonly bool[] IsSword = ItemFactory.CreateNamedSet(nameof(IsSword)).Description("Whether this item is considered a sword").RegisterBoolSet();

	/// <summary> Whether this type should grant the "Timber" achievement. </summary>
	public static readonly bool[] Timber = ItemFactory.CreateBoolSet();

	/// <summary> Whether this NPC is associated with the Corruption or Crimson biomes. </summary>
	public static readonly bool[] IsCorrupt = NPCFactory.CreateNamedSet(nameof(IsCorrupt)).Description("Whether this NPC is associated with the Corruption or Crimson biomes").RegisterBoolSet(
		NPCID.CorruptBunny, NPCID.CorruptGoldfish, NPCID.Corruptor, NPCID.CorruptPenguin, NPCID.CorruptSlime, NPCID.BigMimicCorruption, NPCID.DesertGhoulCorruption, 
		NPCID.PigronCorruption, NPCID.SandsharkCorrupt, NPCID.Crimera, NPCID.Crimslime, NPCID.CrimsonAxe, NPCID.CursedHammer, NPCID.CrimsonBunny, NPCID.CrimsonGoldfish, 
		NPCID.CrimsonPenguin, NPCID.BigMimicCrimson, NPCID.DesertGhoulCrimson, NPCID.PigronCrimson, NPCID.SandsharkCrimson);

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileFactory.CreateIntSet();

	/// <summary> Determines the draw height of this basic tile. </summary>
	public static readonly int[] FrameHeight = TileFactory.CreateIntSet();

	/// <summary> Whether this type is a dungeon wall variant. </summary>
	public static readonly bool[] DungeonWall = WallFactory.CreateBoolSet(WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonTileUnsafe, WallID.BlueDungeonUnsafe, WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe, WallID.GreenDungeonUnsafe, WallID.PinkDungeonSlabUnsafe, WallID.PinkDungeonTileUnsafe, WallID.PinkDungeonUnsafe);

	/// <summary> Whether this type blocks light. </summary>
	public static readonly bool[] WallBlocksLight = WallFactory.CreateBoolSet();
}