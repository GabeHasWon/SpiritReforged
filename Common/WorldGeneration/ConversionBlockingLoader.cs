using MonoMod.Cil;

namespace SpiritReforged.Common.WorldGeneration;

public sealed class ConversionBlockingLoader : ILoadable
{
	public void Load(Mod mod)
	{
		IL_WorldGen.SpreadInfectionToNearbyTile += AddInfectionBlockingSystem;
		On_WorldGen.SpreadGrass += BlockGrassSpread;
	}

	private static void AddInfectionBlockingSystem(ILContext il)
	{
		ILCursor c = new(il);
		ILLabel label = null;

		c.GotoNext(x => x.MatchCall<WorldGen>("CountNearBlocksTypes"));
		c.GotoNext(MoveType.After, x => x.MatchRet());
		label = c.MarkLabel();

		c.EmitLdloc1();
		c.EmitLdloc2();

		c.EmitDelegate(BlockHardmodeInfection);
		c.EmitBrtrue(label);
		c.EmitRet();
	}

	private static bool BlockHardmodeInfection(int x, int y) => !CanCorrupt(x, y);

	private static void BlockGrassSpread(On_WorldGen.orig_SpreadGrass orig, int i, int j, int dirt, int grass, bool repeat, TileColorCache color)
	{
		if ((TileID.Sets.Corrupt[grass] || TileID.Sets.Crimson[grass]) && !CanCorrupt(i, j))
			return; //Skip orig

		orig(i, j, dirt, grass, repeat, color);
	}

	public static bool CanCorrupt(int x, int y)
	{
		for (int c = 0; c < SpiritSets.AntiInfectionStrength.Length; c++)
		{
			int range = SpiritSets.AntiInfectionStrength[c];
			if (range > 0 && WorldGen.CountNearBlocksTypes(x, y, range, 0, c) > 0)
				return false;
		}

		return true;
	}

	public void Unload() { }
}