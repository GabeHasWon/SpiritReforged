namespace SpiritReforged.Common;

[ReinitializeDuringResizeArrays]
public class SpiritSets : ModSystem
{
	/// <summary> Whether this item is considered a sword and should be compatible with the sword stand.<para/>
	/// Added in <see cref="Content.Forest.Stand.SwordStand.RegisterIsSword"/>. </summary>
	public static readonly bool[] IsSword = ItemID.Sets.Factory.CreateNamedSet(nameof(IsSword)).Description("Whether this item is considered a sword").RegisterBoolSet(ItemID.Zenith, ItemID.TerraBlade,
		ItemID.NightsEdge, ItemID.TrueNightsEdge, ItemID.Excalibur, ItemID.TrueExcalibur, ItemID.Arkhalis, ItemID.Terragrim, ItemID.TheHorsemansBlade, ItemID.BloodyMachete, ItemID.Swordfish,
		ItemID.ObsidianSwordfish, ItemID.JoustingLance, ItemID.HallowJoustingLance, ItemID.ShadowJoustingLance, ItemID.PiercingStarlight);

	/// <summary> Whether this item is a gem. </summary>
	public static readonly bool[] Gemstone = ItemID.Sets.Factory.CreateNamedSet(nameof(Gemstone)).Description("Whether this item is a gem").RegisterBoolSet(ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire, 
		ItemID.Ruby, ItemID.Emerald, ItemID.Diamond, ItemID.Amber);

	/// <summary> Whether this type should grant the "Timber" achievement. </summary>
	public static readonly bool[] Timber = ItemID.Sets.Factory.CreateBoolSet();

	/// <summary> The type that this item will transform into when Shimmered, according to <see cref="Content.Aether.Items.ScryingLens"/>. </summary>
	public static readonly int[] ShimmerDisplayResult = ItemID.Sets.Factory.CreateNamedSet(nameof(ShimmerDisplayResult)).Description("The Shimmer item type that Scrying Lens will display").RegisterIntSet();

	/// <summary> Whether this NPC is associated with the Corruption or Crimson biomes. </summary>
	public static readonly bool[] IsCorrupt = NPCID.Sets.Factory.CreateNamedSet(nameof(IsCorrupt)).Description("Whether this NPC is associated with the Corruption or Crimson biomes")
		.RegisterBoolSet(NPCID.CorruptBunny, NPCID.CorruptGoldfish, NPCID.Corruptor, NPCID.CorruptPenguin, NPCID.CorruptSlime, NPCID.BigMimicCorruption, NPCID.DesertGhoulCorruption, 
		NPCID.PigronCorruption, NPCID.SandsharkCorrupt, NPCID.Crimera, NPCID.Crimslime, NPCID.CrimsonAxe, NPCID.CursedHammer, NPCID.CrimsonBunny, NPCID.CrimsonGoldfish, 
		NPCID.CrimsonPenguin, NPCID.BigMimicCrimson, NPCID.DesertGhoulCrimson, NPCID.PigronCrimson, NPCID.SandsharkCrimson, NPCID.EaterofSouls, NPCID.VileSpit,
		NPCID.VileSpitEaterOfWorlds, NPCID.DevourerBody, NPCID.DevourerHead, NPCID.DevourerTail, NPCID.FaceMonster);

	/// <summary> Whether this type converts into the provided type when mowed with a lawnmower. </summary>
	public static readonly int[] Mowable = TileID.Sets.Factory.CreateIntSet();

	/// <summary> Determines the draw height of this basic tile. </summary>
	public static readonly int[] FrameHeight = TileID.Sets.Factory.CreateIntSet();

	/// <summary> Whether this tile type blocks infection in a small area, and the square range it does so. </summary>
	public static readonly int[] AntiInfectionStrength = TileID.Sets.Factory.CreateIntSet();

	/// <summary> Whether this tile type allows liquid to pass through unconditionally. </summary>
	public static readonly bool[] AllowsLiquid = TileID.Sets.Factory.CreateBoolSet();

	public static readonly bool[] IsPlanterBox = TileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, nameof(IsPlanterBox)).Description("Whether a tile is a planter box or not.")
		.RegisterBoolSet(false, TileID.PlanterBox);

	/// <summary> Whether this type is a dungeon wall variant. </summary>
	public static readonly bool[] DungeonWall = WallID.Sets.Factory.CreateBoolSet(WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonTileUnsafe, WallID.BlueDungeonUnsafe, 
		WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe, WallID.GreenDungeonUnsafe, WallID.PinkDungeonSlabUnsafe, WallID.PinkDungeonTileUnsafe, WallID.PinkDungeonUnsafe);

	/// <summary> Whether this type blocks light. </summary>
	public static readonly bool[] WallBlocksLight = WallID.Sets.Factory.CreateBoolSet();

	/// <summary> The rules on which solid tiles this foreground wall can merge with, if any. </summary>
	public static readonly Func<int, int, bool>[] ForegroundMergeFunc = WallID.Sets.Factory.CreateCustomSet<Func<int, int, bool>>(null);
}