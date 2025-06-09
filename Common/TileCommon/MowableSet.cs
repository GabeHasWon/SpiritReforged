using ILLogger;
using MonoMod.Cil;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Allows modded tiles to be mowed using a lawnmower. See <see cref="SpiritSets.Mowable"/>. </summary>
internal class MowableSet : ILoadable
{
	public void Load(Mod mod) => IL_Player.MowGrassTile += ModifyMowType;

	private static void ModifyMowType(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(x => x.MatchCall<WorldGen>("CanKillTile")))
		{
			SpiritReforgedMod.Instance.LogIL("Custom Mowable Tiles", "Method 'WorldGen.CanKillTile' not found.");
			return;
		}

		if (!c.TryGotoNext(MoveType.After, x => x.MatchLdcI4(0)))
		{
			SpiritReforgedMod.Instance.LogIL("Custom Mowable Tiles", "Instruction 'LdcI4 0' not found.");
			return;
		}

		c.EmitLdloc0();
		c.EmitDelegate(Convert);
	}

	private static ushort Convert(int toType, Point pt)
	{
		int newType = SpiritSets.Mowable[Main.tile[pt.X, pt.Y].TileType];
		return (ushort)((newType != -1) ? newType : toType);
	}

	public void Unload() { }
}