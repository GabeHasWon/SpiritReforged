using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Can be applied to ModTiles. </summary>
public interface IModifySmartTarget
{
	/// <summary> Modifies the final coordinates of smart cursor when in use. </summary>
	public void ModifyTarget(ref int x, ref int y);
}

internal class ModifySmartTarget : ModSystem
{
	private static readonly HashSet<IModifySmartTarget> Loaded = [];

	public override void Load() => IL_SmartCursorHelper.SmartCursorLookup += PostCoords;
	private static void PostCoords(ILContext il)
	{
		var c = new ILCursor(il);

		if (c.TryGotoNext(MoveType.After, x => x.MatchCall<SmartCursorHelper>("Step_StaffOfRegrowth")))
		{
			c.Emit(OpCodes.Ldloca_S, (byte)8);
			c.Emit(OpCodes.Ldloca_S, (byte)9);
			c.EmitDelegate(Modify);
		}
		else
		{
			SpiritReforgedMod.Instance.LogIL("Modify Smart Target", "Method 'Step_StaffOfRegrowth' not found.");
		}
	}

	public override void SetStaticDefaults()
	{
		foreach (var tile in Mod.GetContent<ModTile>())
		{
			if (tile is IModifySmartTarget target)
				Loaded.Add(target);
		}
	}

	private static void Modify(ref int x, ref int y)
	{
		if (x == -1 || y == -1)
			return;

		foreach (var inst in Loaded)
			inst.ModifyTarget(ref x, ref y);
	}
}