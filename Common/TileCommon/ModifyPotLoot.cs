namespace SpiritReforged.Common.TileCommon;

/// <summary> Modifies vanilla pots to use additional <see cref="LootTable"/> drops. </summary>
internal class ModifyPotLoot : ILoadable
{
	public void Load(Mod mod) => On_WorldGen.SpawnThingsFromPot += ModifyLoot;
	private static void ModifyLoot(On_WorldGen.orig_SpawnThingsFromPot orig, int i, int j, int x2, int y2, int style)
	{
		orig(i, j, x2, y2, style);
		TileLootHandler.Resolve(i, j, TileID.Pots, 0, 0);
	}

	public void Unload() { }
}