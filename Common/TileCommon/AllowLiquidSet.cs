namespace SpiritReforged.Common.TileCommon;

/// <summary> Allows liquid to pass through specific tiles. See <see cref="SpiritSets.AllowsLiquid"/>. </summary>
internal class AllowLiquidSet : ILoadable
{
	public void Load(Mod mod) => On_Liquid.UpdateLiquid += TrickSolid;

	private static void TrickSolid(On_Liquid.orig_UpdateLiquid orig)
	{
		CacheSet(out var values);

		orig();

		foreach (int type in values)
			Main.tileSolidTop[type] = false;
	}

	private static void CacheSet(out HashSet<int> values)
	{
		values = [];
		for (int type = 0; type < TileLoader.TileCount; type++)
		{
			if (SpiritSets.AllowsLiquid[type] && !Main.tileSolidTop[type])
			{
				values.Add(type);
				Main.tileSolidTop[type] = true;
			}
		}
	}

	public void Unload() { }
}