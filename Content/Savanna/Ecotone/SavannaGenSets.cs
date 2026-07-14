namespace SpiritReforged.Content.Savanna.Ecotone;

[ReinitializeDuringResizeArrays]
internal static class SavannaGenSets
{
	public static readonly bool[] SavannaCanReplace = TileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, "SavannaCanReplace")
		.Description("Which tiles the Savanna can replace.")
		.RegisterBoolSet(defaultState: false, TileID.Dirt, TileID.Grass, TileID.ClayBlock, TileID.CrimsonGrass, TileID.CorruptGrass, TileID.Stone, TileID.JungleGrass, TileID.Mud);

	public static readonly bool[] SavannaCanClear = TileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, "SavannaCanClear")
		.Description("Which tiles the Savanna can clear.")
		.RegisterBoolSet(defaultState: false, TileID.Dirt, TileID.Grass, TileID.ClayBlock, TileID.CrimsonGrass, TileID.CorruptGrass, TileID.Stone, TileID.Mud, TileID.JungleGrass, 
			TileID.Sand, TileID.HardenedSand, TileID.Trees, TileID.Ebonstone, TileID.Crimstone, TileID.Iron, TileID.Copper, TileID.Tin, TileID.Lead, TileID.Silver, TileID.Platinum, 
			TileID.Gold, TileID.Tungsten, TileID.ClayBlock);

	public static readonly bool[] SavannaCanReplaceWall = WallID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, "SavannaCanReplaceWall")
		.Description("Which walls the Savanna can or cannot clear. By default, this includes dungeon walls disabled only, though Corruption and Crimson walls are also disabled seperately.")
		.RegisterBoolSet(defaultState: true, WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonTileUnsafe, WallID.BlueDungeonUnsafe, WallID.PinkDungeonSlabUnsafe, 
			WallID.PinkDungeonUnsafe, WallID.PinkDungeonTileUnsafe, WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe, WallID.GreenDungeonUnsafe);
}
