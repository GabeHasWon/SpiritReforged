using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace SpiritReforged.Common.WorldGeneration;

internal class ConversionBlockingSystem : ModSystem
{
	public override void Load()
	{
		IL_WorldGen.SpreadInfectionToNearbyTile += AddInfectionBlockingSystem;
		IL_WorldGen.hardUpdateWorld += AddInfectionBlocking_GrassSpread;

		On_WorldGen.GetTileTypeCountByCategory += AddUnCorruptTiles;
	}

	private int AddUnCorruptTiles(On_WorldGen.orig_GetTileTypeCountByCategory orig, int[] tileTypeCounts, TileScanGroup group)
	{
		int count = orig(tileTypeCounts, group);

		if (group is TileScanGroup.Corruption or TileScanGroup.Crimson or TileScanGroup.TotalGoodEvil)
			foreach ((int type, int reduce) in SpiritSets.NegativeTileCorruption)
				count -= tileTypeCounts[type] * reduce;

		return count;
	}

	public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
	{
		int count = 0;

		foreach ((int type, int reduce) in SpiritSets.NegativeTileCorruption)
			count += tileCounts[type] * reduce;

		Main.SceneMetrics.BloodTileCount -= count;
		Main.SceneMetrics.EvilTileCount -= count;
	}

	private void AddInfectionBlocking_GrassSpread(ILContext il)
	{
		ILCursor c = new(il);

		// Block corruption spread
		if (!IndividualCreateHook(c, OpCodes.Bgt))
			return;

		// Block crimson spread
		IndividualCreateHook(c, OpCodes.Bgt);
	}

	private static bool IndividualCreateHook(ILCursor c, OpCode brOp)
	{
		// Go to the check that looks for Sunflowers
		if (!c.TryGotoNext(x => x.MatchCall<WorldGen>(nameof(WorldGen.CountNearBlocksTypes))))
		{
			SpiritReforgedMod.Instance.LogIL("Conversion Blocking System", "Method 'WorldGen.CountNearBlocksTypes' not found.");
			return false;
		}

		ILLabel label = null;

		// Match the label that skips the conversion
		if (!c.TryGotoNext(MoveType.After, x => x.Match(brOp, out label)) || label is null)
		{
			SpiritReforgedMod.Instance.LogIL("Conversion Blocking System", $"{brOp.Name} break opcode not found.");
			return false;
		}

		int index = c.Index;
		int xLoc = -1;
		int yLoc = -1;

		// Match the local index for y, then x.
		if (!c.TryGotoPrev(x => x.MatchLdloc(out yLoc)))
		{
			SpiritReforgedMod.Instance.LogIL("Conversion Blocking System", "Ldloc for Y iterator not found.");
			return false;
		}

		if (!c.TryGotoPrev(x => x.MatchLdloc(out xLoc)))
		{
			SpiritReforgedMod.Instance.LogIL("Conversion Blocking System", "Ldloc for X iterator not found.");
			return false;
		}

		c.Index = index;

		c.Emit(OpCodes.Ldloc_S, (byte)xLoc);
		c.Emit(OpCodes.Ldloc_S, (byte)yLoc);
		c.EmitDelegate(BlockInfection);
		c.Emit(OpCodes.Brfalse, label);
		return true;
	}

	private void AddInfectionBlockingSystem(ILContext il)
	{
		ILCursor c = new(il);
		IndividualCreateHook(c, OpCodes.Ble);
	}

	public static bool BlockInfection(int x, int y)
	{
		for (int i = x - SpiritSets.MaxInfectionCheckRange; i < x + SpiritSets.MaxInfectionCheckRange; ++i)
		{
			for (int j = y - SpiritSets.MaxInfectionCheckRange; j < y + SpiritSets.MaxInfectionCheckRange; ++j)
			{
				int distance = Math.Max(Math.Abs(i - x), Math.Abs(j - y));
				Tile tile = Main.tile[x, y];

				if (tile.HasTile && SpiritSets.TileBlocksInfectionSpread.TryGetValue(tile.TileType, out int range) && distance < range)
					return true;
			}
		}

		return false;
	}
}
