namespace SpiritReforged.Common;

[ReinitializeDuringResizeArrays]
public class SpiritSets : ModSystem
{
	internal static SetFactory ItemFactory = new(ItemLoader.ItemCount, "SpiritItems");
	internal static SetFactory NPCFactory = new(NPCLoader.NPCCount, "SpiritNPCs");
	internal static SetFactory TileFactory = new(TileLoader.TileCount, "SpiritTiles");
	internal static SetFactory WallFactory = new(WallLoader.WallCount, "SpiritWalls");

	/// <summary> Whether this item is considered a sword and should be compatible with the sword stand.<para/>
	/// Added in <see cref="Content.Forest.Stand.SwordStand.RegisterIsSword"/>. </summary>
	public static readonly bool[] IsSword = ItemFactory.CreateNamedSet(nameof(IsSword)).Description("Whether this item is considered a sword").RegisterBoolSet(ItemID.Zenith, ItemID.TerraBlade,
		ItemID.NightsEdge, ItemID.TrueNightsEdge, ItemID.Excalibur, ItemID.TrueExcalibur, ItemID.Arkhalis, ItemID.Terragrim, ItemID.TheHorsemansBlade, ItemID.BloodyMachete, ItemID.Swordfish,
		ItemID.ObsidianSwordfish, ItemID.JoustingLance, ItemID.HallowJoustingLance, ItemID.ShadowJoustingLance, ItemID.PiercingStarlight);

	/// <summary> Whether this item is considered a katana for subclass delegations. </summary>
	public static readonly bool[] IsKatana = ItemFactory.CreateNamedSet(nameof(IsKatana)).Description("Whether this item is considered a katana for subclass delegations").RegisterBoolSet();

	/// <summary> Whether this item is Jem. </summary>
	public static readonly bool[] Gemstone = ItemFactory.CreateNamedSet(nameof(Gemstone)).Description("Whether this item is a gem").RegisterBoolSet(ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire, 
		ItemID.Ruby, ItemID.Emerald, ItemID.Diamond, ItemID.Amber);

	/// <summary> Whether this type should grant the "Timber" achievement. </summary>
	public static readonly bool[] Timber = ItemFactory.CreateBoolSet();

	/// <summary> The type that this item will transform into when Shimmered, according to <see cref="Content.Aether.Items.ScryingLens"/>. </summary>
	public static readonly int[] ShimmerDisplayResult = ItemFactory.CreateNamedSet(nameof(ShimmerDisplayResult)).Description("The Shimmer item type that Scrying Lens will display").RegisterIntSet();

	/// <summary> Whether this NPC is associated with the Corruption or Crimson biomes. </summary>
	public static readonly bool[] IsCorrupt = NPCFactory.CreateNamedSet(nameof(IsCorrupt)).Description("Whether this NPC is associated with the Corruption or Crimson biomes")
		.RegisterBoolSet(NPCID.CorruptBunny, NPCID.CorruptGoldfish, NPCID.Corruptor, NPCID.CorruptPenguin, NPCID.CorruptSlime, NPCID.BigMimicCorruption, NPCID.DesertGhoulCorruption, 
		NPCID.PigronCorruption, NPCID.SandsharkCorrupt, NPCID.Crimera, NPCID.Crimslime, NPCID.CrimsonAxe, NPCID.CursedHammer, NPCID.CrimsonBunny, NPCID.CrimsonGoldfish, 
		NPCID.CrimsonPenguin, NPCID.BigMimicCrimson, NPCID.DesertGhoulCrimson, NPCID.PigronCrimson, NPCID.SandsharkCrimson, NPCID.EaterofSouls, NPCID.VileSpit,
		NPCID.VileSpitEaterOfWorlds, NPCID.DevourerBody, NPCID.DevourerHead, NPCID.DevourerTail, NPCID.FaceMonster);

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileFactory.CreateIntSet();

	/// <summary> Determines the draw height of this basic tile. </summary>
	public static readonly int[] FrameHeight = TileFactory.CreateIntSet();

	/// <summary> Whether this tile type blocks infection in a small area, and the square range it does so. </summary>
	public static readonly int[] AntiInfectionStrength = TileFactory.CreateIntSet();

	/// <summary> Whether this tile type allows liquid to pass through unconditionally. </summary>
	public static readonly bool[] AllowsLiquid = TileFactory.CreateBoolSet();

	/// <summary> Whether this type is a dungeon wall variant. </summary>
	public static readonly bool[] DungeonWall = WallFactory.CreateBoolSet(WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonTileUnsafe, WallID.BlueDungeonUnsafe, 
		WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe, WallID.GreenDungeonUnsafe, WallID.PinkDungeonSlabUnsafe, WallID.PinkDungeonTileUnsafe, WallID.PinkDungeonUnsafe);

	/// <summary> Whether this type blocks light. </summary>
	public static readonly bool[] WallBlocksLight = WallFactory.CreateBoolSet();
}